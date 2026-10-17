// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Orleans.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    static readonly TimeSpan _watchdogStartupDelay = TimeSpan.FromSeconds(5);

    IGrainTimer? _watchdogTimer;
    bool _countsFromStorage;

    JobsOptions JobsOptionsOrDefault => ServiceProvider.GetService<IOptions<JobsOptions>>()?.Value ?? new JobsOptions();

    TimeSpan WatchdogInterval => JobsOptionsOrDefault.JobWatchdogInterval;

    TimeSpan WatchdogStartupDelay => WatchdogInterval > TimeSpan.Zero && WatchdogInterval != Timeout.InfiniteTimeSpan
        ? TimeSpan.FromTicks(Math.Min(WatchdogInterval.Ticks, _watchdogStartupDelay.Ticks))
        : WatchdogInterval;

    /// <summary>
    /// Looks at the steps in storage and finalizes the job when all of them are done, restarts steps whose owner is gone
    /// and keeps the window full.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    /// <remarks>
    /// The counters the job keeps while steps report to it are a cache. A report that was lost leaves the job running with
    /// every step finished; storage is the record of what happened, so it is what the job is finalized from.
    /// </remarks>
    internal async Task RunWatchdog()
    {
        if (State.Status is not JobStatus.Running || State.DrainPending || !JobIsPrepared())
        {
            DisposeWatchdog();
            return;
        }

        using var scope = _logger.BeginJobScope(JobId, JobKey);
        try
        {
            var counted = await Storage.JobSteps.CountByStatus(JobId);
            if (!TryGetCounts(counted, out var counts))
            {
                _logger.WatchdogCouldNotCountSteps();
                return;
            }

            var notDone = counts.Where(_ => !IsDone(_.Key)).Sum(_ => _.Value);
            if (notDone == 0 && _pendingRetries.Count == 0)
            {
                await FinalizeJobWhoseStepsAreAllDone(counts);
                return;
            }

            await RestartStepsWhoseOwnerIsGone();
            await FillWindow();
        }
        catch (Exception ex)
        {
            _logger.WatchdogFailed(ex);
        }
    }

    /// <summary>
    /// Gets the addresses of the silos that are part of the cluster, or null when that cannot be told.
    /// </summary>
    /// <returns>The silo addresses, formatted as a step records them as the owner of its lease.</returns>
    /// <remarks>
    /// Used to decide whether the owner of an expired step lease is gone. A job that cannot tell answers null, and no
    /// step with a lease is restarted.
    /// </remarks>
    protected virtual async Task<ISet<string>?> GetActiveSilos()
    {
        var hosts = await GrainFactory.GetGrain<IManagementGrain>(0).GetHosts(true);
        return hosts.Keys.Select(_ => _.ToString()).ToHashSet(StringComparer.Ordinal);
    }

    static bool IsDone(JobStepStatus status) =>
        status is JobStepStatus.CompletedSuccessfully or JobStepStatus.CompletedWithFailure or JobStepStatus.Failed or JobStepStatus.Unreachable;

    static bool TryGetCounts(Catch<IReadOnlyDictionary<JobStepStatus, int>> counted, out IReadOnlyDictionary<JobStepStatus, int> counts)
    {
        counts = new Dictionary<JobStepStatus, int>();
        try
        {
            if (counted.TryGetException(out _) || !counted.TryGetResult(out var result) || result is null)
            {
                return false;
            }
            counts = result;
            return true;
        }
        catch (InvalidOperationException)
        {
            // A storage that answered nothing at all.
            return false;
        }
    }

    static bool LeaseExpired(JobStepState step, DateTimeOffset now, TimeSpan leaseTimeout)
    {
        // A step written before leases existed has none; it is left alone for one lease period after its last change.
        var lastSign = step.Lease?.LastHeartbeat
            ?? step.StatusChanges.Select(_ => (DateTimeOffset?)_.Occurred).LastOrDefault()
            ?? DateTimeOffset.MinValue;
        return now - lastSign >= leaseTimeout;
    }

    void EnsureWatchdog(TimeSpan dueTime)
    {
        var interval = WatchdogInterval;
        if (_watchdogTimer is not null || interval <= TimeSpan.Zero || interval == Timeout.InfiniteTimeSpan)
        {
            return;
        }

        _watchdogTimer = this.RegisterGrainTimer(
            _ => RunWatchdog(),
            new GrainTimerCreationOptions { DueTime = dueTime, Period = interval });
    }

    void DisposeWatchdog()
    {
        _watchdogTimer?.Dispose();
        _watchdogTimer = null;
    }

    /// <summary>
    /// Recounts the progress of a running job from the steps in storage when it activates.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    /// <remarks>
    /// After this, an outcome from a step this activation does not track is one that storage already accounts for,
    /// and is acknowledged without counting it a second time.
    /// </remarks>
    async Task CountStepsFromStorageAfterActivation()
    {
        if (State.Status is not JobStatus.Running || IsStaged)
        {
            return;
        }

        try
        {
            if (!TryGetCounts(await Storage.JobSteps.CountByStatus(JobId), out var counts))
            {
                return;
            }

            ReconcileProgressFromCounts(counts);
            _countsFromStorage = true;
        }
        catch (Exception ex)
        {
            _logger.WatchdogFailed(ex);
        }
    }

    void ReconcileProgressFromCounts(IReadOnlyDictionary<JobStepStatus, int> counts)
    {
        // A step that succeeded is normally removed from storage, so what is not in storage and not accounted for as
        // anything else succeeded.
        var failed = counts.GetValueOrDefault(JobStepStatus.Failed) + counts.GetValueOrDefault(JobStepStatus.CompletedWithFailure);
        var unreachable = counts.GetValueOrDefault(JobStepStatus.Unreachable);
        var notSucceeded = counts.Where(_ => _.Key is not JobStepStatus.CompletedSuccessfully).Sum(_ => _.Value);
        State.Progress.FailedSteps = failed;
        State.Progress.UnreachableSteps = unreachable;
        State.Progress.SuccessfulSteps = Math.Max(0, State.Progress.TotalSteps - notSucceeded);
    }

    async Task FinalizeJobWhoseStepsAreAllDone(IReadOnlyDictionary<JobStepStatus, int> counts)
    {
        _logger.FinalizingJobLeftRunningAfterAllStepsCompleted();
        if (IsStaged)
        {
            _ = await ReconcileProgressFromJobSteps();
        }
        else
        {
            ReconcileProgressFromCounts(counts);
        }

        foreach (var stepId in (_jobStepGrains ?? []).Keys.ToList())
        {
            await UnsubscribeJobStep(stepId);
        }
        _jobStepGrains?.Clear();
        _started.Clear();
        _inFlight.Clear();
        DisposeWatchdog();

        _ = await HandleCompletionResult(await HandleCompletion());
    }

    async Task RestartStepsWhoseOwnerIsGone()
    {
        var leaseTimeout = JobsOptionsOrDefault.StepLeaseTimeout;
        if (leaseTimeout <= TimeSpan.Zero || leaseTimeout == Timeout.InfiniteTimeSpan)
        {
            return;
        }

        var running = await Storage.JobSteps.GetForJob(JobId, JobStepStatus.Scheduled, JobStepStatus.Running);
        if (running.TryGetException(out var exception))
        {
            _logger.WatchdogFailed(exception);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var expired = running.AsT0.Where(step => LeaseExpired(step, now, leaseTimeout)).ToList();
        if (expired.Count == 0)
        {
            return;
        }

        var activeSilos = expired.Exists(_ => _.Lease is not null) ? await GetActiveSilos() : null;
        var restartable = expired
            .Where(step => step.Lease is null || activeSilos?.Contains(step.Lease.Owner) == false)
            .ToDictionary(step => step.Id.JobStepId, GetJobStepGrain);
        if (restartable.Count == 0)
        {
            return;
        }

        _logger.RestartingStepsWhoseOwnerIsGone(restartable.Count);
        foreach (var stepId in restartable.Keys)
        {
            _jobStepGrains ??= [];
            _jobStepGrains[stepId] = restartable[stepId];
            _started.Add(stepId);
        }
        _ = await ResumeCurrentStage(restartable, reportAlreadyRunning: false);
    }
}
