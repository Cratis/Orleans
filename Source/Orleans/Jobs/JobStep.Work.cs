// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Workers;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Orleans.Jobs;

public abstract partial class JobStep<TRequest, TResult, TState>
{
    /// <inheritdoc/>
    protected override async Task<PerformWorkResult<JobStepResult>> PerformWork()
    {
        // The work carries its own run; reading the activation's current run from here could pick up another run.
        var run = JobStepRun.Current;
        if (run?.Cancellation.IsCancellationRequested != false)
        {
            return Cancelled();
        }

        var cancellationToken = run.Cancellation;
        try
        {
            await throttle.AcquireAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
        {
            return Cancelled();
        }

        try
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Cancelled();
            }

            using var scope = logger.BeginJobStepScope(Identifier, Name, JobStepStatus.Running);
            Result<TState, ActivationCallbackRejected> running;
            try
            {
                running = await TryInvokeOnActivation(MarkRunningAndSnapshot);
            }
            catch (Exception ex)
            {
                // A step that cannot even be marked running still owes its job an outcome.
                return await RecordFailure(run, ex);
            }
            if (!running.TryGetResult(out var currentState))
            {
                return Cancelled();
            }

            var performed = await PerformStep(currentState, cancellationToken);
            return await performed.Match(
                result => Record(run, result, null),
                exception => RecordFailure(run, exception));
        }
        finally
        {
            // Always release throttle slot after work completes (success or failure)
            try
            {
                throttle.Release();
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    static PerformWorkResult<JobStepResult> Cancelled() => new() { Error = PerformWorkError.Cancelled };

    static PerformWorkResult<JobStepResult> ToPerformWorkResult(JobStepResult outcome, Exception? exception) =>
        outcome.TryGetError(out var error)
            ? new()
            {
                Result = outcome,
                Exception = exception!,
                Error = error.Cancelled ? PerformWorkError.Cancelled : PerformWorkError.PerformingWorkError
            }
            : new() { Result = outcome };

    async Task<TState> MarkRunningAndSnapshot()
    {
        // A failed Running write does not stop the work; the terminal write records the outcome.
        _ = await WriteStatusChange(JobStepStatus.Running);
        return Snapshot();
    }

    TState Snapshot()
    {
        DeepCopier<TState>? copier;
        try
        {
            copier = ServiceProvider.GetService<DeepCopier<TState>>();
        }
        catch (Exception ex)
        {
            throw new MissingStateCopier(typeof(TState), ex);
        }
        return (copier ?? throw new MissingStateCopier(typeof(TState), null)).Copy(State);
    }

    Task<PerformWorkResult<JobStepResult>> RecordFailure(JobStepRun run, Exception exception)
    {
        logger.HandleUnexpectedPerformJobStepFailure(exception);
        return Record(run, JobStepResult.Failed(PerformJobStepError.Failed(exception)), exception);
    }

    async Task<PerformWorkResult<JobStepResult>> Record(JobStepRun run, JobStepResult outcome, Exception? exception)
    {
        // A rejected record means a stop or deactivation closed the run; its drainer decides what is reported.
        try
        {
            _ = await TryInvokeOnActivation(() => Task.FromResult(RecordOutcome(run, outcome)));
        }
        catch (Exception ex)
        {
            logger.FailedUnexpectedly(ex);
            return new() { Result = outcome, Exception = ex, Error = PerformWorkError.WorkerError };
        }
        return ToPerformWorkResult(outcome, exception);
    }

    bool RecordOutcome(JobStepRun run, JobStepResult outcome)
    {
        if (!run.TryRecord(outcome))
        {
            return false;
        }

        // Until a drain begins, the work owns delivery. Once a drain began, the drainer delivers the recorded outcome.
        if (!run.IsDraining && run.TryClaimDelivery())
        {
            StartDelivery(run, outcome);
        }
        return true;
    }
}
