// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Monads;

namespace Cratis.Orleans.Jobs;

public abstract partial class JobStep<TRequest, TResult, TState>
{
    /// <inheritdoc/>
    /// <remarks>
    /// Cancels the work and waits for it and its activation callbacks to settle, bounded by
    /// <see cref="JobsOptions.StepStopTimeout"/>. Then the outcome is reported to the job without this call waiting
    /// for the job: an outcome the work recorded before the stop stays, otherwise the step reports itself stopped.
    /// A step that does not settle in time answers <see cref="JobStepError.StopTimedOut"/> and is not quiescent; it
    /// still reports its outcome once, when the work does settle. A stop of a run that is already stopping joins
    /// that stop.
    /// </remarks>
    public async Task<Result<JobStepError>> Stop(bool removing)
    {
        using var scope = logger.BeginJobStepScope(State);
        return await StopWithinDeadline(removing, JobExecutionGeneration.Take());
    }

    /// <inheritdoc/>
    public Task OnJobStopped() => AcceptStop(false, JobExecutionGeneration.Take());

    /// <inheritdoc/>
    public Task OnJobRemoved() => AcceptStop(true, JobExecutionGeneration.Take());

    static async Task<bool> Drained(JobStepRun run, CancellationToken deadline) =>
        await Settled(run.Startup, deadline) &&
        await Settled(run.WorkerSettled, deadline) &&
        await Settled(run.CallbacksSettled, deadline);

    static async Task<bool> Settled(Task task, CancellationToken deadline)
    {
        try
        {
            await task.WaitAsync(deadline);
            return true;
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception) when (task.IsFaulted)
        {
            // A faulted task has settled; its failure is reported where it happened.
            return true;
        }
    }

    static bool IsLaterGeneration(long? generation, long? than) =>
        generation is { } value && (than is not { } other || value > other);

    Task AcceptStop(bool removing, long? generation)
    {
        // The job is notified from inside its own stop and cannot take the step's report until it returns, so
        // accept once cancellation is requested and let the stop finish on the activation.
        _ = StopWithinDeadline(removing, generation);
        return Task.CompletedTask;
    }

    async Task<Result<JobStepError>> StopWithinDeadline(bool removing, long? generation)
    {
        // The deadline starts before anything is waited for: the run, the gate, the work and its callbacks.
        using var deadline = new CancellationTokenSource(_stopTimeout);
        return await StopRun(removing, generation, deadline.Token);
    }

    async Task<Result<JobStepError>> StopRun(bool removing, long? generation, CancellationToken deadline)
    {
        // A stop of an earlier execution that arrives after the job resumed must not stop the resumed execution.
        if (IsOfEarlierExecution(generation))
        {
            logger.IgnoringStopOfEarlierExecution(generation!.Value);
            return Result.Success<JobStepError>();
        }

        // Any other stop wins over a start deferred until the previous stop settled.
        _pendingStart = null;

        // A late notification must not replace a terminal outcome or report a second outcome to the job.
        if (State.Status is JobStepStatus.CompletedSuccessfully or JobStepStatus.CompletedWithFailure or JobStepStatus.Failed)
        {
            return Result.Success<JobStepError>();
        }
        if (!removing && State.Status is JobStepStatus.Stopped && _run is not { IsSettled: false })
        {
            logger.AlreadyStopped();
            TryDeactivateOnIdle();
            return Result.Success<JobStepError>();
        }

        logger.Stopping();

        // Nothing here yields until the run carries its stop, so no start or second stop can see it settled.
        var run = _run is { IsSettled: false } live ? live : StartStopOnlyRun(generation);
        run.BeginDrain(removing ? JobStepRunPhase.Removing : JobStepRunPhase.Stopping);
        run.Stop ??= FinishStop(run);

        var elapsed = Stopwatch.StartNew();
        if (await Settled(run.StopClaimed, deadline))
        {
            return Result.Success<JobStepError>();
        }

        // The stop goes on: it reports the outcome once the work settles, whenever that is.
        run.CloseAdmission();
        logger.StopTimedOut(Name, JobStepId, JobId, elapsed.Elapsed, run.DescribeUnsettled());
        return JobStepError.StopTimedOut;
    }

    JobStepRun StartStopOnlyRun(long? generation)
    {
        // A stop with no run to stop reports for the execution that asked for it.
        var run = new JobStepRun(++_lastRunId, TaskScheduler.Current, generation);
        run.Started();
        _run = run;
        return run;
    }

    async Task FinishStop(JobStepRun run)
    {
        try
        {
            // Not bounded: a stop that timed out for its caller still owes the job exactly one outcome.
            _ = await Drained(run, CancellationToken.None);
            await _gate.WaitAsync();
            try
            {
                // A deactivated activation reports nothing; recovery runs the step again.
                if (IsCurrent(run) && await ClaimStopOutcome(run) is { } outcome)
                {
                    StartDelivery(run, outcome);
                }
            }
            finally
            {
                _gate.Release();
                run.Claimed();
            }

            // Whoever delivers - the work or this stop - a removed step is removed once that delivery is done.
            await run.DeliverySettled;
            if (run.Phase is JobStepRunPhase.Removing)
            {
                _ = await WriteStatusChangeUnderGate(run, JobStepStatus.Removing);
            }
        }
        catch (Exception ex)
        {
            logger.FailedUnexpectedly(ex);
            _ = await WriteStatusChangeUnderGate(run, JobStepStatus.Failed, ex.GetAllMessages(), ex.StackTrace);
        }
        finally
        {
            run.Claimed();
        }

        // A start deferred until this stop settled runs in a turn of its own, once nothing of this run is in flight.
        if (_pendingStart is not null && IsCurrent(run))
        {
            _pendingStartTimer ??= this.RegisterGrainTimer(
                StartPending,
                new GrainTimerCreationOptions { DueTime = TimeSpan.Zero, Period = Timeout.InfiniteTimeSpan });
        }
    }

    bool IsOfEarlierExecution(long? generation) =>
        generation is { } stop &&
        (_pendingStart?.Generation ?? (_run is { IsSettled: false } live ? live.Generation : null)) is { } current &&
        stop < current;

    async Task StartPending(CancellationToken cancellationToken)
    {
        _pendingStartTimer?.Dispose();
        _pendingStartTimer = null;
        if (_pendingStart is not { } pending || _deactivated)
        {
            return;
        }

        _pendingStart = null;
        using var scope = logger.BeginJobStepScope(State);
        var started = await StartRun(pending.JobGrainId, pending.Generation);
        if (!started.TryGetError(out var error))
        {
            return;
        }

        if (error is not (StartJobStepError.Completed or StartJobStepError.AlreadyStarted))
        {
            // The job counts the step as started in its new execution, so a start that cannot happen must reach it.
            logger.DeferredStartFailed(error);
            await ReportDeferredStartFailure(pending, error);
        }
        else
        {
            logger.DeferredStartNotNeeded(error);
        }
        TryDeactivateOnIdle();
    }

    async Task ReportDeferredStartFailure(PendingStart pending, StartJobStepError error)
    {
        try
        {
            using var generation = JobExecutionGeneration.Carry(pending.Generation);
            var failure = PerformJobStepError.Failed([$"The step could not be started again after its stop: {error}"], string.Empty);
            if ((await GetJob(pending.JobGrainId).OnStepFailed(JobStepId, JobStepResult.Failed(failure))).TryGetError(out var jobError))
            {
                logger.FailedReportJobStepFailure(jobError);
            }
        }
        catch (Exception ex)
        {
            logger.FailedUnexpectedly(ex);
        }
    }

    async Task<JobStepResult?> ClaimStopOutcome(JobStepRun run)
    {
        run.CloseAdmission();

        // The work delivers an outcome it recorded while it was running; an earlier success stays a success.
        if (!run.TryClaimDelivery())
        {
            return null;
        }
        if (run.Outcome is null && State.IsPrepared)
        {
            run.TryRecord(await CreateStoppedOutcome());
        }
        return run.Outcome;
    }

    async Task<JobStepResult> CreateStoppedOutcome()
    {
        try
        {
            var result = await CreateCancelledResultFromCurrentState(State);
            return JobStepResult.Failed(result is not null
                ? PerformJobStepError.CancelledWithPartialResult(result)
                : PerformJobStepError.CancelledWithNoResult());
        }
        catch (Exception ex)
        {
            // Without a partial result the stop must still reach the job, or the job never completes its stop.
            logger.FailedUnexpectedly(ex);
            return JobStepResult.Failed(PerformJobStepError.CancelledWithNoResult());
        }
    }

    async Task DrainForDeactivation(CancellationToken deadline)
    {
        if (_run is not { } run)
        {
            return;
        }

        // Nothing is recorded or reported: the step stays Scheduled or Running for recovery to run again.
        run.BeginDrain(JobStepRunPhase.Deactivating);
        var elapsed = Stopwatch.StartNew();
        if (!await Drained(run, deadline))
        {
            logger.DeactivationTimedOut(Name, JobStepId, JobId, elapsed.Elapsed, run.DescribeUnsettled());
        }
        run.CloseAdmission();
    }

    void StartDelivery(JobStepRun run, JobStepResult outcome)
    {
        // Delivery waits on the job, so it runs as its own activation task that no stop, deactivation or start awaits.
        run.DeliverySettled = Task.Factory.StartNew(
            () => Deliver(run, outcome),
            CancellationToken.None,
            TaskCreationOptions.DenyChildAttach,
            run.Scheduler).Unwrap();
    }

    async Task Deliver(JobStepRun run, JobStepResult outcome)
    {
        // Never report for a run the activation no longer owns; recovery runs the step again.
        if (!IsCurrent(run))
        {
            return;
        }

        // The delivery inherits the context of whatever started it, so it always states the run's own generation.
        using var generation = JobExecutionGeneration.Carry(run.Generation);
        try
        {
            if (outcome.TryGetError(out var error))
            {
                _ = await ReportFailureToJob(run, error);
            }
            else
            {
                await DeliverSuccess(run, outcome);
            }
        }
        catch (Exception ex)
        {
            logger.FailedUnexpectedly(ex);
            if (outcome.TryGetError(out _))
            {
                // A failure that cannot reach the job must not leave the step looking live.
                _ = await WriteStatusChangeUnderGate(run, JobStepStatus.Failed, ex.GetAllMessages(), ex.StackTrace);
            }
        }
        finally
        {
            if (IsCurrent(run))
            {
                TryDeactivateOnIdle();
            }
        }
    }

    async Task DeliverSuccess(JobStepRun run, JobStepResult outcome)
    {
        // Success is persisted before it is reported.
        if ((await WriteStatusChangeUnderGate(run, JobStepStatus.CompletedSuccessfully)).TryGetError(out var errorChangingStatus))
        {
            // A success that could not be persisted because the activation no longer owns the run is not reported:
            // the step is still Running in storage, and recovery runs it again.
            if (!IsCurrent(run))
            {
                return;
            }
            logger.PerformingWorkFailedPersistState(errorChangingStatus);
            _ = await WriteStatusChangeUnderGate(run, JobStepStatus.Failed, ["Error reporting job step succeeded"]);
        }
        if ((await _job.OnStepSucceeded(JobStepId, outcome)).TryGetError(out var errorReportingSuccess))
        {
            logger.FailedReportJobStepSuccess(errorReportingSuccess);
            _ = await WriteStatusChangeUnderGate(run, JobStepStatus.Failed, ["Error reporting job step succeeded"]);
        }
    }

    async Task<Result<JobStepError>> ReportFailureToJob(JobStepRun? run, PerformJobStepError error)
    {
        // A failure or stop is persisted only after the job accepted it.
        var reported = error.Cancelled
            ? await _job.OnStepStopped(JobStepId, JobStepResult.Failed(error))
            : await _job.OnStepFailed(JobStepId, JobStepResult.Failed(error));
        if (reported.TryGetError(out var jobError))
        {
            logger.FailedReportJobStepFailure(jobError);
            return JobStepError.FailedToReportToJob;
        }

        return await WriteStatusChangeUnderGate(
            run,
            error.Cancelled ? JobStepStatus.Stopped : JobStepStatus.CompletedWithFailure,
            error.ErrorMessages,
            error.ExceptionStackTrace);
    }

    /// <summary>
    /// A start that waits for the stop of an earlier execution to settle.
    /// </summary>
    /// <param name="JobGrainId">The job that asked for the start.</param>
    /// <param name="Generation">The job's execution generation the start belongs to.</param>
    sealed record PendingStart(GrainId JobGrainId, long Generation);
}
