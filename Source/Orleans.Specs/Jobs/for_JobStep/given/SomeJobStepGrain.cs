// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;
using Cratis.Orleans.Workers;
using Microsoft.Extensions.Logging;

namespace Cratis.Orleans.Jobs.for_JobStep.given;

/// <summary>
/// A concrete job step whose performed work is observable from a specification.
/// </summary>
/// <param name="state">The persistent state.</param>
/// <param name="throttle">The throttle.</param>
/// <param name="logger">The logger.</param>
public class SomeJobStepGrain(
    [PersistentState(nameof(JobStepState), WellKnownGrainStorageProviders.JobSteps)] IPersistentState<JobStepState> state,
    IJobStepThrottle throttle,
    ILogger<SomeJobStepGrain> logger) : JobStep<SomeRequest, object, JobStepState>(state, throttle, logger), ISomeJobStepGrain
{
    int _workRuns;

    /// <summary>
    /// Gets the completion that is resolved when <see cref="PerformStep"/> has run.
    /// </summary>
    public TaskCompletionSource Performed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets or sets whether <see cref="PerformStep"/> holds its work open until <see cref="Gate"/> is released -
    /// how a specification arranges a step that is genuinely running right now.
    /// </summary>
    public bool HoldPerformOpen { get; set; }

    /// <summary>
    /// Gets the gate a held-open <see cref="PerformStep"/> waits on.
    /// </summary>
    public TaskCompletionSource Gate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets or sets whether the work waits before entering the base implementation.
    /// </summary>
    public bool HoldWorkBeforePerformWork { get; set; }

    /// <summary>
    /// Gets the signal that the background task has entered the work override.
    /// </summary>
    public TaskCompletionSource WorkEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets how many times background work has been launched.
    /// </summary>
    public int WorkRuns => _workRuns;

    /// <summary>
    /// Gets the signal that background work has been launched a second time.
    /// </summary>
    public TaskCompletionSource SecondWorkRunEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the gate holding work before the base implementation inspects cancellation.
    /// </summary>
    public TaskCompletionSource WorkGate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets or sets the partial result derived from the current state on cancellation.
    /// </summary>
    public object? CancelledResult { get; set; }

    /// <summary>
    /// Gets or sets the gate that deriving the cancelled result waits on; null derives it at once.
    /// </summary>
    public TaskCompletionSource? HoldCancelledResult { get; set; }

    /// <summary>
    /// Gets the signal that the step was asked for its cancelled result.
    /// </summary>
    public TaskCompletionSource CancelledResultRequested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the signal that the token supplied to the performed step has been cancelled.
    /// </summary>
    public TaskCompletionSource PerformCancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets or sets work the performed step does before it is held open and returns, given the step itself and
    /// the token supplied to the step.
    /// </summary>
    public Func<SomeJobStepGrain, CancellationToken, Task>? Work { get; set; }

    /// <summary>
    /// Gets or sets the result the performed step returns, overriding the result derived from its token.
    /// </summary>
    public JobStepResult? PerformedResult { get; set; }

    /// <summary>
    /// Gets or sets whether the performed step succeeds even when its token has been cancelled.
    /// </summary>
    public bool SucceedEvenIfCancelled { get; set; }

    /// <summary>
    /// Gets the state supplied to the performed step.
    /// </summary>
    public JobStepState ReceivedState { get; private set; }

    /// <summary>
    /// Gets the live state of the activation.
    /// </summary>
    public JobStepState LiveState => State;

    /// <summary>
    /// Gets the token supplied to the performed step.
    /// </summary>
    public CancellationToken PerformToken { get; private set; }

    /// <summary>
    /// Gets the completion containing the outcome of the background work, including thrown failures.
    /// </summary>
    public TaskCompletionSource<PerformWorkResult<JobStepResult>> WorkCompleted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the job grain this step reports to - a substitute, wired to accept every report.
    /// </summary>
    public IJob ReportedJob { get; } = CreateAcceptingJob();

    /// <summary>
    /// Runs a callback on the activation, as step work would.
    /// </summary>
    /// <typeparam name="T">Type of the callback's result.</typeparam>
    /// <param name="callback">The callback.</param>
    /// <param name="queueCancellationToken">Cancels the callback while it waits to run.</param>
    /// <returns>The callback's result or why it was refused.</returns>
    public Task<Result<T, ActivationCallbackRejected>> OnActivation<T>(Func<Task<T>> callback, CancellationToken queueCancellationToken = default) =>
        TryInvokeOnActivation(callback, queueCancellationToken);

    /// <summary>
    /// Runs a callback on the activation, as step work would.
    /// </summary>
    /// <param name="callback">The callback.</param>
    /// <param name="queueCancellationToken">Cancels the callback while it waits to run.</param>
    /// <returns>Success or why it was refused.</returns>
    public Task<Result<ActivationCallbackRejected>> OnActivation(Func<Task> callback, CancellationToken queueCancellationToken = default) =>
        TryInvokeOnActivation(callback, queueCancellationToken);

    /// <summary>
    /// Writes a progress checkpoint carrying the given message. Only call from inside an activation callback.
    /// </summary>
    /// <param name="message">The progress message to checkpoint.</param>
    /// <returns>The outcome of the write.</returns>
    public Task<Cratis.Monads.Catch> Checkpoint(string message)
    {
        State.Progress.Message = new(message);
        return WriteStateAsync();
    }

    /// <inheritdoc/>
    protected override Task<Result<PrepareJobStepError>> PrepareStep(SomeRequest request) => Task.FromResult(Result<PrepareJobStepError>.Success());

    /// <inheritdoc/>
    protected override IJobStep<SomeRequest, object, JobStepState> GetReferenceToSelf() => this;

    /// <inheritdoc/>
    protected override IJob GetJob(GrainId jobGrainId) => ReportedJob;

    /// <inheritdoc/>
    protected override ValueTask InitializeState(SomeRequest request) => ValueTask.CompletedTask;

    /// <inheritdoc/>
    protected override async Task<PerformWorkResult<JobStepResult>> PerformWork()
    {
        try
        {
            if (Interlocked.Increment(ref _workRuns) == 2)
            {
                SecondWorkRunEntered.TrySetResult();
            }
            WorkEntered.TrySetResult();
            if (HoldWorkBeforePerformWork)
            {
                await WorkGate.Task;
            }
            var result = await base.PerformWork();
            WorkCompleted.TrySetResult(result);
            return result;
        }
        catch (Exception error)
        {
            WorkCompleted.TrySetException(error);
            throw;
        }
    }

    /// <inheritdoc/>
    protected override async Task<Catch<JobStepResult>> PerformStep(JobStepState currentState, CancellationToken cancellationToken)
    {
        PerformToken = cancellationToken;
        ReceivedState = currentState;
        cancellationToken.Register(() => PerformCancelled.TrySetResult());
        Performed.TrySetResult();
        if (Work is not null)
        {
            await Work(this, cancellationToken);
        }
        if (HoldPerformOpen)
        {
            await Gate.Task;
        }

        if (PerformedResult is not null)
        {
            return Catch<JobStepResult>.Success(PerformedResult);
        }

        return Catch<JobStepResult>.Success(cancellationToken.IsCancellationRequested && !SucceedEvenIfCancelled
            ? JobStepResult.Failed(PerformJobStepError.CancelledWithNoResult())
            : JobStepResult.Succeeded());
    }

    /// <inheritdoc/>
    protected override async ValueTask<object?> CreateCancelledResultFromCurrentState(JobStepState currentState)
    {
        CancelledResultRequested.TrySetResult();
        if (HoldCancelledResult is { } hold)
        {
            await hold.Task;
        }
        return CancelledResult;
    }

    static IJob CreateAcceptingJob()
    {
        var job = Substitute.For<IJob>();
        job.OnStepSucceeded(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>()).Returns(Result<JobError>.Success());
        job.OnStepFailed(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>()).Returns(Result<JobError>.Success());
        job.OnStepStopped(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>()).Returns(Result<JobError>.Success());
        return job;
    }
}
