// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace Cratis.Orleans.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    readonly HashSet<JobStepId> _accountedByDrain = [];
    Dictionary<JobStepId, IJobStep>? _drainTargets;
    bool _drainReconcilesFromSteps;
    IGrainTimer? _drainTimer;

    TimeSpan StepStopTimeout =>
        ServiceProvider.GetService<IOptions<JobsOptions>>()?.Value?.StepStopTimeout ?? new JobsOptions().StepStopTimeout;

    /// <inheritdoc/>
    /// <remarks>
    /// A step counts as drained only when it answered the drain itself. A report the step sends meanwhile counts the
    /// step if nothing has yet, but never settles the drain. Implementations of
    /// <see cref="OnStepCompletedOrStopped"/> and <see cref="OnStopped"/> must tolerate running again for the same
    /// step or stop: a drain completed after the job went away runs them for what it finds.
    /// </remarks>
    public async Task<JobDrainStatus> StopAndDrain(TimeSpan timeout)
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        return await Drain(BoundedByResponseTimeout(timeout));
    }

    /// <summary>
    /// Completes a drain that an earlier activation began and did not settle.
    /// </summary>
    /// <remarks>
    /// A grain timer, so activation does not wait for the steps, and the drain holds a turn of its own.
    /// </remarks>
    void CompleteDrainAfterActivation()
    {
        _drainTimer = this.RegisterGrainTimer(
            async _ =>
            {
                _drainTimer?.Dispose();
                _drainTimer = null;
                if (!State.DrainPending)
                {
                    return;
                }

                using var scope = _logger.BeginJobScope(JobId, JobKey);
                try
                {
                    _logger.CompletedPendingDrainAfterActivation(await Drain(BoundedByResponseTimeout(StepStopTimeout)));
                }
                catch (Exception ex)
                {
                    _logger.FailedCompletingPendingDrain(ex);
                }
            },
            new GrainTimerCreationOptions { DueTime = TimeSpan.Zero, Period = Timeout.InfiniteTimeSpan });
    }

    async Task<JobDrainStatus> Drain(TimeSpan timeout)
    {
        var elapsed = Stopwatch.StartNew();
        if (ExecutionGeneration() == 0)
        {
            return JobDrainStatus.Drained;
        }
        if (State.Status is JobStatus.CompletedSuccessfully or JobStatus.CompletedWithFailures)
        {
            return JobDrainStatus.Completed;
        }
        if (State.Status is JobStatus.Failed)
        {
            return await DrainStepsOfFailedJob(timeout, elapsed);
        }

        if (!State.DrainPending)
        {
            await FindStepsToDrain();

            // Asked again after the drain settled: nothing has changed, so nothing is written or run again.
            if (State.Status is JobStatus.Stopped && State.Progress.IsStopped && _drainTargets!.Count == 0)
            {
                _drainTargets = null;
                return JobDrainStatus.Drained;
            }
            await BeginDrain();
        }
        else if (_drainTargets is null)
        {
            await FindStepsToDrain();
        }

        foreach (var (stepId, result) in await DrainEach(_drainTargets!, timeout, elapsed))
        {
            await AccountForDrainedStep(stepId, result);
        }
        if (_drainTargets!.Count > 0)
        {
            _logger.DrainTimedOut(_drainTargets.Count);
            _ = await WriteState();
            return JobDrainStatus.TimedOut;
        }

        return await SettleDrain();
    }

    async Task FindStepsToDrain()
    {
        var targets = new Dictionary<JobStepId, IJobStep>();
        if (!_hasLiveStepTracking)
        {
            // An activation that has not run its steps knows them only from storage, stopped ones included.
            _jobStepGrains = await GetIdAndGrainReferenceForNonCompletedJobSteps();
            _drainReconcilesFromSteps = true;
        }
        else
        {
            // A step whose report this activation accepted may still be persisting its outcome.
            targets = await GetIdAndGrainReferenceJobSteps(JobStepStatus.Scheduled, JobStepStatus.Running, JobStepStatus.Unknown);
        }

        foreach (var (stepId, step) in _jobStepGrains ?? [])
        {
            targets[stepId] = step;
        }

        // Steps in a stage that never started have nothing to drain.
        _drainTargets = targets.Where(_ => IsInStartedStage(_.Key)).ToDictionary(_ => _.Key, _ => _.Value);
        _hasLiveStepTracking = true;
    }

    bool IsInStartedStage(JobStepId stepId) =>
        !IsStaged ||
        (_jobStepStages.TryGetValue(stepId, out var stage) && State.Progress.Stages.Any(_ => _.Stage == stage && _.IsStarted));

    async Task BeginDrain()
    {
        // The stop and the pending drain are one write, so a job that goes away from here completes the drain.
        _logger.BeginningDrain();
        var written = await WriteOrUndo(() =>
        {
            if (State.Status is not (JobStatus.Stopped or JobStatus.Removing))
            {
                StatusChanged(JobStatus.Stopped);
            }
            State.DrainPending = true;
        });
        if (written.TryGetException(out var exception))
        {
            // No step is drained under a stop that a restart would not find.
            ExceptionDispatchInfo.Throw(exception);
        }

        _startStepsTimer?.Dispose();
        _startStepsTimer = null;
    }

    async Task<List<(JobStepId StepId, JobStepDrainResult Result)>> DrainEach(Dictionary<JobStepId, IJobStep> steps, TimeSpan timeout, Stopwatch elapsed)
    {
        var generation = ExecutionGeneration();
        var drained = new List<(JobStepId StepId, JobStepDrainResult Result)>();
        foreach (var chunk in steps.ToList().Chunk(MaxConcurrentJobStepStartups))
        {
            var remaining = timeout > elapsed.Elapsed ? timeout - elapsed.Elapsed : TimeSpan.Zero;
            var results = await Task.WhenAll(chunk.Select(step => TryDrain(step.Key, step.Value, generation, remaining)));
            drained.AddRange(results.Where(_ => _.Result is { Drained: true }).Select(_ => (_.StepId, _.Result!)));
        }
        return drained;
    }

    async Task<(JobStepId StepId, JobStepDrainResult? Result)> TryDrain(JobStepId stepId, IJobStep step, long generation, TimeSpan timeout)
    {
        try
        {
            return (stepId, await (step as IJobStepDrain ?? step.AsReference<IJobStepDrain>()).Drain(generation, timeout));
        }
        catch (Exception ex)
        {
            // A step hosted by a release without drain, or one that cannot be reached, is not drained.
            _logger.FailedDrainingJobStep(ex, stepId);
            return (stepId, null);
        }
    }

    async Task AccountForDrainedStep(JobStepId stepId, JobStepDrainResult result)
    {
        _drainTargets!.Remove(stepId);
        _accountedByDrain.Add(stepId);

        // A step whose report this activation accepted has been counted and is no longer tracked.
        if (_jobStepGrains?.ContainsKey(stepId) != true)
        {
            return;
        }

        switch (result.Status)
        {
            case JobStepStatus.CompletedSuccessfully:
                OutcomeCounted(stepId, succeeded: true);
                State.Progress.SuccessfulSteps++;
                CountStepOutcomeInStage(stepId, succeeded: true);
                break;
            case JobStepStatus.CompletedWithFailure or JobStepStatus.Failed:
                OutcomeCounted(stepId, succeeded: false);
                State.Progress.FailedSteps++;
                CountStepOutcomeInStage(stepId, succeeded: false);
                break;
            default:
                State.Progress.StoppedSteps++;
                break;
        }

        if (result.Outcome is { } outcome)
        {
            try
            {
                await OnStepCompletedOrStopped(stepId, outcome);
            }
            catch (Exception ex)
            {
                _logger.FailedHandlingDrainedJobStep(ex, stepId);
            }
        }
        await UnsubscribeJobStep(stepId);
        _jobStepGrains.Remove(stepId);
    }

    async Task<JobDrainStatus> SettleDrain()
    {
        // Counts kept by an activation that did not see every step's outcome are recounted from what the steps persisted.
        if (_drainReconcilesFromSteps || !(State.Progress.IsStopped || State.Progress.IsCompleted))
        {
            await ReconcileProgressFromJobSteps(countStopped: true);
        }

        State.DrainPending = false;
        if ((await HandleCompletionResult(await HandleCompletion())).IsSuccess)
        {
            _drainTargets = null;
            _drainReconcilesFromSteps = false;
            _logger.Drained();
            return JobDrainStatus.Drained;
        }

        // Not durable, so not settled: the next call settles again.
        State.DrainPending = true;
        return JobDrainStatus.TimedOut;
    }

    async Task<JobDrainStatus> DrainStepsOfFailedJob(TimeSpan timeout, Stopwatch elapsed)
    {
        var steps = await GetIdAndGrainReferenceJobSteps(JobStepStatus.Scheduled, JobStepStatus.Running, JobStepStatus.Unknown);
        var drained = await DrainEach(steps, timeout, elapsed);
        return drained.Count == steps.Count ? JobDrainStatus.Completed : JobDrainStatus.TimedOut;
    }

    TimeSpan BoundedByResponseTimeout(TimeSpan timeout)
    {
        // Reports queued behind the drain's turn must not time out waiting for it, so the turn ends well within
        // the response timeout: half of it, less a margin for the hooks and writes that follow the wait.
        var responseTimeout = ServiceProvider.GetService<IOptions<SiloMessagingOptions>>()?.Value?.ResponseTimeout ?? new SiloMessagingOptions().ResponseTimeout;
        var bound = (responseTimeout / 2) - (responseTimeout / 10);
        if (timeout == Timeout.InfiniteTimeSpan || timeout > bound)
        {
            return bound;
        }
        return timeout < TimeSpan.Zero ? TimeSpan.Zero : timeout;
    }
}
