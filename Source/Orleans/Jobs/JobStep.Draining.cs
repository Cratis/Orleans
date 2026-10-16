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
    /// A step that does not settle in time answers <see cref="JobStepError.StopTimedOut"/>, records nothing and is
    /// not quiescent.
    /// </remarks>
    public async Task<Result<JobStepError>> Stop(bool removing)
    {
        using var scope = logger.BeginJobStepScope(State);
        return await StopWithinDeadline(removing);
    }

    /// <inheritdoc/>
    public Task OnJobStopped() => AcceptStop(false);

    /// <inheritdoc/>
    public Task OnJobRemoved() => AcceptStop(true);

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

    Task AcceptStop(bool removing)
    {
        // The job is notified from inside its own stop and cannot take the step's report until it returns, so
        // accept once cancellation is requested and let the stop finish on the activation.
        _ = StopWithinDeadline(removing);
        return Task.CompletedTask;
    }

    async Task<Result<JobStepError>> StopWithinDeadline(bool removing)
    {
        // The deadline starts before anything is waited for: the run, the gate, the work and its callbacks.
        using var deadline = new CancellationTokenSource(_stopTimeout);
        return await StopRun(removing, deadline.Token);
    }

    async Task<Result<JobStepError>> StopRun(bool removing, CancellationToken deadline)
    {
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

        // A step without live work still reports its stop through a run, so a new start waits for that report.
        var run = _run is { IsSettled: false } live ? live : StartStopOnlyRun();
        run.BeginDrain(removing ? JobStepRunPhase.Removing : JobStepRunPhase.Stopping);
        var elapsed = Stopwatch.StartNew();
        try
        {
            if (!await Drained(run, deadline))
            {
                return StopTimedOut(run, elapsed);
            }

            await _gate.WaitAsync(deadline);
            (bool Claimed, JobStepResult? Outcome) stop;
            try
            {
                stop = await ClaimStopOutcome(run);
            }
            finally
            {
                _gate.Release();
            }

            if (stop.Outcome is not null)
            {
                StartDelivery(run, stop.Outcome);
            }
            else if (removing && stop.Claimed)
            {
                _ = await WriteStatusChangeUnderGate(run, JobStepStatus.Removing);
            }
            return Result.Success<JobStepError>();
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            return StopTimedOut(run, elapsed);
        }
        catch (Exception ex)
        {
            logger.FailedUnexpectedly(ex);
            _ = await WriteStatusChangeUnderGate(null, JobStepStatus.Failed, ex.GetAllMessages(), ex.StackTrace);
            return JobStepError.Unknown;
        }
    }

    JobStepRun StartStopOnlyRun()
    {
        var run = new JobStepRun(++_lastRunId, TaskScheduler.Current);
        run.Started();
        _run = run;
        return run;
    }

    async Task<(bool Claimed, JobStepResult? Outcome)> ClaimStopOutcome(JobStepRun run)
    {
        run.CloseAdmission();

        // The work delivers an outcome it recorded while it was running; an earlier success stays a success.
        if (!run.TryClaimDelivery())
        {
            return (false, null);
        }
        if (run.Outcome is null && State.IsPrepared)
        {
            run.TryRecord(await CreateStoppedOutcome());
        }
        return (true, run.Outcome);
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

    Result<JobStepError> StopTimedOut(JobStepRun run, Stopwatch elapsed)
    {
        run.CloseAdmission();
        logger.StopTimedOut(Name, JobStepId, JobId, elapsed.Elapsed, run.DescribeUnsettled());
        return JobStepError.StopTimedOut;
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
            // A step whose job is removed is removed once its outcome was delivered or could not be, whichever
            // of the work or the stop owns the delivery.
            if (run.Phase is JobStepRunPhase.Removing)
            {
                _ = await WriteStatusChangeUnderGate(run, JobStepStatus.Removing);
            }
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
}
