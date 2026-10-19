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
    /// <remarks>
    /// Stops the step like a stop through its job, then waits - bounded by <paramref name="timeout"/> - until the
    /// outcome is claimed and persists it under the gate. Waiting never closes admission: the stop does that at its
    /// own deadline. The outcome is still reported to the job, which ignores it once the drain has accounted for it.
    /// A persisted success removes the step's record, so its result cannot be read back from step storage later.
    /// A drain that reaches an activation while it deactivates answers not drained once its wait ends: deactivating
    /// leaves the step to recovery rather than claiming its outcome. The next drain reaches the new activation.
    /// </remarks>
    public async Task<JobStepDrainResult> Drain(long generation, TimeSpan timeout)
    {
        using var scope = logger.BeginJobStepScope(State);

        // The generation comes as an argument; one inherited from the caller must not flow into the stop or its report.
        _ = JobExecutionGeneration.Take();

        // The job drains its current execution; nothing deferred may start after it. The activation stayed only for
        // that start, so once drained it deactivates as it would have after its stop.
        _deactivateWhenDrained |= _pendingStart is not null;
        _pendingStart = null;
        var result = await DrainRun(generation, timeout);
        if (result.Drained && _deactivateWhenDrained)
        {
            _deactivateWhenDrained = false;
            TryDeactivateOnIdle();
        }
        return result;
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

    static JobStepStatus StatusOf(JobStepResult? outcome)
    {
        if (outcome is null)
        {
            return JobStepStatus.Stopped;
        }
        if (!outcome.TryGetError(out var error))
        {
            return JobStepStatus.CompletedSuccessfully;
        }
        return error.Cancelled ? JobStepStatus.Stopped : JobStepStatus.CompletedWithFailure;
    }

    static bool IsLaterGeneration(long? generation, long? than) =>
        generation is { } value && (than is not { } other || value > other);

    async Task<JobStepDrainResult> DrainRun(long generation, TimeSpan timeout)
    {
        if (_run is not { IsSettled: false } && State.Status is JobStepStatus.CompletedSuccessfully or JobStepStatus.CompletedWithFailure or JobStepStatus.Failed or JobStepStatus.Stopped)
        {
            return new(true, State.Status, _run?.Outcome);
        }

        // Nothing here yields until the run carries its stop, as for any other stop.
        var run = _run is { IsSettled: false } live ? live : StartStopOnlyRun(generation);
        if (!run.IsDraining)
        {
            run.BeginDrain(JobStepRunPhase.Stopping);
            run.Stop ??= FinishStop(run);
            _ = ClaimWithinStopTimeout(run);
        }

        using var wait = new CancellationTokenSource(timeout);
        if (!await Settled(run.StopClaimed, wait.Token))
        {
            logger.NotDrainedInTime(timeout);
            return new(false, State.Status, null);
        }

        await _gate.WaitAsync();
        try
        {
            if (!IsCurrent(run) || run.StopError is not null)
            {
                return new(false, State.Status, null);
            }
            return await PersistDrainedOutcome(run);
        }
        finally
        {
            _gate.Release();
        }
    }

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
        if (_deactivated)
        {
            return JobStepError.Unknown;
        }

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
        run.Stop ??= FinishStop(run, deadline);

        if (!await ClaimWithinDeadline(run, deadline))
        {
            return _job is null && State.IsPrepared ? JobStepError.FailedToReportToJob : JobStepError.StopTimedOut;
        }

        return run.StopError is { } error ? error : Result.Success<JobStepError>();
    }

    async Task ClaimWithinStopTimeout(JobStepRun run)
    {
        using var deadline = new CancellationTokenSource(_stopTimeout);
        _ = await ClaimWithinDeadline(run, deadline.Token);
    }

    async Task<bool> ClaimWithinDeadline(JobStepRun run, CancellationToken deadline)
    {
        var elapsed = Stopwatch.StartNew();
        if (await Settled(run.StopClaimed, deadline))
        {
            return true;
        }

        // The stop goes on: it reports the outcome once the work settles, whenever that is.
        run.CloseAdmission();
        logger.StopTimedOut(Name, JobStepId, JobId, elapsed.Elapsed, run.DescribeUnsettled());
        return false;
    }

    async Task<JobStepDrainResult> PersistDrainedOutcome(JobStepRun run)
    {
        // A terminal status is already persisted - a success is written before it is reported.
        if (State.Status is JobStepStatus.CompletedSuccessfully or JobStepStatus.CompletedWithFailure or JobStepStatus.Failed)
        {
            run.OutcomePersisted = true;
            return new(true, State.Status, run.Outcome);
        }

        var status = StatusOf(run.Outcome);

        // A step that was never prepared has no record to write; there is nothing of it to persist.
        if (!run.OutcomePersisted && State.Status != status && State.IsPrepared)
        {
            run.Outcome!.TryGetError(out var failure);
            if ((await WriteStatusChange(status, failure?.ErrorMessages, failure?.ExceptionStackTrace)).TryGetError(out _))
            {
                return new(false, State.Status, null);
            }
        }
        run.OutcomePersisted = true;
        return new(true, status, run.Outcome);
    }

    JobStepRun StartStopOnlyRun(long? generation)
    {
        // A stop with no run to stop reports for the execution that asked for it.
        var run = new JobStepRun(++_lastRunId, TaskScheduler.Current, generation);
        run.Started();
        _run = run;
        return run;
    }

    async Task FinishStop(JobStepRun run, CancellationToken? parentLookupDeadline = null)
    {
        try
        {
            // A cold drain accounts for its returned outcome itself and needs no parent. An ordinary stop must
            // find its parent before claiming anything, but must never wait for the parent's callback here.
            if (parentLookupDeadline is { } deadline && State.IsPrepared && !await PrepareStopDelivery(run, deadline))
            {
                return;
            }

            // Not bounded: a stop that timed out for its caller still owes the job exactly one outcome.
            _ = await Drained(run, CancellationToken.None);
            await _gate.WaitAsync();
            try
            {
                // A deactivated activation reports nothing; recovery runs the step again.
                if (IsCurrent(run))
                {
                    var outcome = await ClaimStopOutcome(run);
                    if (_job is not null && outcome is not null)
                    {
                        StartDelivery(run, outcome);
                    }
                }
            }
            finally
            {
                _gate.Release();
                run.Claimed();
            }

            // Whoever delivers - the work or this stop - a removed step is removed once that delivery is done. A stop
            // the job never acknowledged leaves the run no longer current, and the step is kept to report it again.
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

            // A lookup failure also releases a deferred start; no failed reservation can strand it.
            if (_pendingStart is not null && IsCurrent(run))
            {
                _pendingStartTimer ??= this.RegisterGrainTimer(
                    StartPending,
                    new GrainTimerCreationOptions { DueTime = TimeSpan.Zero, Period = Timeout.InfiniteTimeSpan });
            }
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
            using var generation = JobExecutionGeneration.Report(pending.Generation);
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

        // The delivery inherits the context of whatever started it, so it always reports the run's own generation
        // and the attempt it belongs to.
        using var generation = JobExecutionGeneration.Report(run.Generation);
        using var attempt = JobStepAttempt.Report(State.Attempt);
        try
        {
            // The job that drained the step took the outcome from the drain; reporting it again changes nothing.
            if (run.OutcomePersisted)
            {
                logger.SkippedReportForDrainedRun();
                return;
            }

            if (outcome.TryGetError(out var error))
            {
                if (error.Cancelled && run.Phase is JobStepRunPhase.Removing)
                {
                    // A removed step deletes itself once this is done, and then nothing is left to report the stop.
                    // So it reports until the job has stored it - a job that is gone answers that it has nothing to.
                    await DeliverUntilAcknowledged(run, () => ReportStopToJob(run, error), untilAcknowledged: true);
                }
                else if (error.Cancelled)
                {
                    // A stop is reported before it is persisted: the step is not done, so it must not look done.
                    _ = await ReportFailureToJob(run, error);
                }
                else
                {
                    await DeliverFailure(run, error);
                }
            }
            else
            {
                await DeliverSuccess(run, outcome);
            }
        }
        catch (Exception ex)
        {
            // What was persisted stays as it is: a delivery that fails is not a failure of the step.
            logger.FailedUnexpectedly(ex);
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

        // Only a started run can succeed, and Start supplied its parent before dispatching the work.
        await DeliverUntilAcknowledged(run, () => _job!.OnStepSucceeded(JobStepId, outcome));
    }

    async Task DeliverFailure(JobStepRun run, PerformJobStepError error)
    {
        // A failure is persisted before it is reported, so a job that never hears of it still finds it in storage.
        var persisted = await WriteStatusChangeUnderGate(run, JobStepStatus.CompletedWithFailure, error.ErrorMessages, error.ExceptionStackTrace);
        if (persisted.TryGetError(out _) && !IsCurrent(run))
        {
            return;
        }

        // Start supplied the parent before dispatching the work that recorded this failure.
        await DeliverUntilAcknowledged(run, () => _job!.OnStepFailed(JobStepId, JobStepResult.Failed(error)));
    }

    async Task<Result<JobError>> ReportStopToJob(JobStepRun run, PerformJobStepError error) =>
        (await ReportFailureToJob(run, error)).TryGetError(out _)
            ? JobError.UnknownError
            : Result<JobError>.Success();

    async Task DeliverUntilAcknowledged(JobStepRun run, Func<Task<Result<JobError>>> deliver, bool untilAcknowledged = false)
    {
        // Bounded by the activation instead when it must be acknowledged: an activation that no longer owns the run
        // stops, and the step's kept state is what is reported again.
        var maxAttempts = untilAcknowledged ? int.MaxValue : Math.Max(1, _options.StepDeliveryMaxAttempts);
        var delay = _options.StepDeliveryRetryDelay;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var reported = await deliver();
                if (!reported.TryGetError(out var jobError))
                {
                    return;
                }
                logger.FailedReportJobStepOutcome(attempt, jobError);
            }
            catch (Exception ex)
            {
                logger.FailedDeliveringJobStepOutcome(ex, attempt);
            }

            if (attempt == maxAttempts || !IsCurrent(run))
            {
                break;
            }

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay);
            }
            delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, Math.Max(_options.MaxStepDeliveryRetryDelay.Ticks, delay.Ticks)));
            if (!IsCurrent(run))
            {
                break;
            }
        }

        // The outcome is persisted; the job's watchdog accounts for a step it was never told about.
        if (!untilAcknowledged)
        {
            logger.GaveUpDeliveringJobStepOutcome(maxAttempts);
        }
    }

    async Task<Result<JobStepError>> ReportFailureToJob(JobStepRun? run, PerformJobStepError error)
    {
        if (_job is null)
        {
            logger.ParentNotResolved(JobId);
            return JobStepError.FailedToReportToJob;
        }

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
