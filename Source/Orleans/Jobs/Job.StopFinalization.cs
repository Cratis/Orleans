// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

public abstract partial class Job<TRequest, TJobState> : IJobStopFinalization
{
    bool _deactivateOnceStopFinalized;

    /// <inheritdoc/>
    public async Task FinalizeStop()
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        try
        {
            if (IsStopFinalized())
            {
                // Rehydration activated this job only to finish its stop. There is nothing left to run, so it must
                // not stay pinned in memory for as long as an active job is.
                DeactivateOnIdleDuringShutdown();
                return;
            }
            if (!NeedsStopFinalization())
            {
                return;
            }
            _deactivateOnceStopFinalized = true;

            // Stop persists Stopped before its steps report, and the stopped hook runs only once they all have. A silo
            // that went away in between left a stop no one would ever finish, so the steps' own records decide.
            if (!(State.Progress.IsStopped || State.Progress.IsCompleted))
            {
                if (await ReconcileProgressFromJobSteps(countStopped: true) is null)
                {
                    return;
                }
                if (!(State.Progress.IsStopped || State.Progress.IsCompleted))
                {
                    await StopStepsThatHaveNotStopped();
                    return;
                }
            }

            _logger.FinalizingStopAfterItsSiloWentAway(State.Status);
            _ = await HandleCompletionResult(await HandleCompletion());
        }
        catch (Exception ex)
        {
            _logger.Failed(ex);
        }
    }

    bool NeedsStopFinalization()
    {
        if (State.Status is not (JobStatus.Stopped or JobStatus.Removing))
        {
            return false;
        }

        // A pending drain is settled by the drain alone, and steps this activation tracks will report on their own.
        if (State.DrainPending || _observers?.Count > 0)
        {
            return false;
        }

        return !IsStopFinalized();
    }

    bool IsStopFinalized() => State.Status is JobStatus.Stopped && State.StopFinalized;

    /// <summary>
    /// Records durably that the stopped hook ran for the current stop.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Rehydration leaves a finalized stop alone without activating the job. Should the write fail, the next
    /// rehydration finalizes the stop once more, which is the safe side: the hook releases what the job holds.
    /// </remarks>
    async Task RecordStopFinalized()
    {
        State.StopFinalized = true;
        if ((await WriteState()).TryGetException(out _))
        {
            return;
        }
        DeactivateIfOnlyActivatedToFinalizeStop();
    }

    /// <summary>
    /// Lets a job that rehydration activated only to finish its stop deactivate once the finished stop is durable.
    /// </summary>
    void DeactivateIfOnlyActivatedToFinalizeStop()
    {
        if (_deactivateOnceStopFinalized && IsStopFinalized())
        {
            _deactivateOnceStopFinalized = false;
            DeactivateOnIdleDuringShutdown();
        }
    }

    async Task StopStepsThatHaveNotStopped()
    {
        // Steps already recorded as stopped were counted from storage; only the ones still to report are tracked, so a
        // late report from a counted step is not counted a second time.
        // Steps in a stage that never started are already counted as stopped and have nothing to stop, so they are left alone.
        _jobStepGrains = (await GetIdAndGrainReferenceJobSteps(JobStepStatus.Scheduled, JobStepStatus.Running, JobStepStatus.Unknown))
            .Where(_ => IsInStartedStage(_.Key))
            .ToDictionary(_ => _.Key, _ => _.Value);
        _hasLiveStepTracking = true;
        _logger.StopIsWaitingForSteps(_jobStepGrains.Count);
        foreach (var (_, jobStepGrain) in _jobStepGrains)
        {
            await SubscribeJobStep(jobStepGrain.AsReference<IJobObserver>());
        }
        await NotifySteps(State.Status is JobStatus.Removing);
    }

    /// <summary>
    /// Runs the stopped hook after a resume that the hook rejected has been put back to stopped.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// The resume hook may have begun work of its own before it failed. The job is stopped again, so the stopped hook
    /// gets to release whatever that work holds, as it would for any other stop.
    /// </remarks>
    async Task RunStoppedHookAfterRejectedResume()
    {
        try
        {
            await OnStopped();
            await RecordStopFinalized();
        }
        catch (Exception ex)
        {
            _logger.Failed(ex);
        }
    }
}
