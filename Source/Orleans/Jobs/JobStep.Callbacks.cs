// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs;

public abstract partial class JobStep<TRequest, TResult, TState>
{
    /// <summary>
    /// Runs a callback on this step's activation from the step's background work, serialized with every other
    /// callback and status write of the step.
    /// </summary>
    /// <typeparam name="T">Type of the callback's result.</typeparam>
    /// <param name="callback">The callback to run on the activation.</param>
    /// <param name="queueCancellationToken">Cancels the callback while it is still waiting to run. A callback that has started always runs to completion.</param>
    /// <returns>The callback's result, or why the step refused to run it.</returns>
    /// <remarks>
    /// <para>
    /// Use this, not a call to the step's own grain reference, to update <see cref="State"/> or write a checkpoint
    /// with <see cref="WriteStateAsync"/> or <see cref="WriteCheckpointDebounced"/>. A call to the own grain
    /// reference is held while the activation deactivates and then reaches a new activation; a callback runs on
    /// this activation, also while it deactivates, and is refused once the run is closed. Stopping and
    /// deactivating wait for started callbacks, so a final checkpoint written while the work is being cancelled
    /// lands before the step stops.
    /// </para>
    /// <para>
    /// The callback runs under the step's gate. Keep it local: mutate state and write it. Never call another grain
    /// or remote system from inside it, never start work it does not await, and never call this method from inside
    /// a callback and wait for the result - the inner callback waits for the outer one to finish. For a checkpoint
    /// that depends on remote work, capture the inputs in one callback, do the remote work outside, and commit in
    /// a second callback, which is refused if the run was stopped in between. Do not hand out references to live
    /// state from a callback; copy what the work needs.
    /// </para>
    /// <para>
    /// Only the background work of a run can use this. Anything else is refused with
    /// <see cref="ActivationCallbackRejected.NoRun"/>, work left over from an earlier run with
    /// <see cref="ActivationCallbackRejected.StaleRun"/>, and work of a run that is closed with
    /// <see cref="ActivationCallbackRejected.Closed"/>. Exceptions thrown by the callback reach the caller.
    /// </para>
    /// </remarks>
    protected Task<Result<T, ActivationCallbackRejected>> TryInvokeOnActivation<T>(Func<Task<T>> callback, CancellationToken queueCancellationToken = default)
    {
        var run = JobStepRun.Current;
        if (run is null)
        {
            return Task.FromResult(Result<T, ActivationCallbackRejected>.Failed(ActivationCallbackRejected.NoRun));
        }
        if (!run.AdmissionOpen)
        {
            logger.RejectedActivationCallback(run.Id, ActivationCallbackRejected.Closed);
            return Task.FromResult(Result<T, ActivationCallbackRejected>.Failed(ActivationCallbackRejected.Closed));
        }
        return Post(run, callback, queueCancellationToken);
    }

    /// <summary>
    /// Runs a callback on this step's activation from the step's background work, serialized with every other
    /// callback and status write of the step.
    /// </summary>
    /// <param name="callback">The callback to run on the activation.</param>
    /// <param name="queueCancellationToken">Cancels the callback while it is still waiting to run. A callback that has started always runs to completion.</param>
    /// <returns>Success, or why the step refused to run the callback.</returns>
    /// <remarks>
    /// The same rules apply as for <see cref="TryInvokeOnActivation{T}(Func{Task{T}}, CancellationToken)"/>.
    /// </remarks>
    protected async Task<Result<ActivationCallbackRejected>> TryInvokeOnActivation(Func<Task> callback, CancellationToken queueCancellationToken = default)
    {
        var result = await TryInvokeOnActivation(
            async () =>
            {
                await callback();
                return true;
            },
            queueCancellationToken);
        return result.TryGetError(out var rejected)
            ? Result.Failed(rejected)
            : Result.Success<ActivationCallbackRejected>();
    }

    async Task<Result<T, ActivationCallbackRejected>> Post<T>(JobStepRun run, Func<Task<T>> callback, CancellationToken queueCancellationToken)
    {
        var queued = new QueuedCallback<T>();
        await using var registration = queueCancellationToken.Register(() => queued.Reject(ActivationCallbackRejected.Cancelled));

        // A callback still queued when admission closes is refused, so its caller never waits on an activation
        // that may be gone.
        await using var closed = run.AdmissionClosed.Register(() => queued.Reject(ActivationCallbackRejected.Closed));
        var posted = Task.Factory.StartNew(
            () => RunQueued(run, callback, queued, queueCancellationToken),
            CancellationToken.None,
            TaskCreationOptions.DenyChildAttach,
            run.Scheduler).Unwrap();
        return await Task.WhenAny(posted, queued.Rejected).Unwrap();
    }

    async Task<Result<T, ActivationCallbackRejected>> RunQueued<T>(JobStepRun run, Func<Task<T>> callback, QueuedCallback<T> queued, CancellationToken queueCancellationToken)
    {
        try
        {
            await _gate.WaitAsync(queueCancellationToken);
        }
        catch (OperationCanceledException)
        {
            return ActivationCallbackRejected.Cancelled;
        }

        try
        {
            // Started or rejected is decided once: a callback cancelled or closed out while queued never runs.
            if (!queued.TryStart())
            {
                return await queued.Rejected;
            }
            if (!ReferenceEquals(run, _run))
            {
                logger.RejectedActivationCallback(run.Id, ActivationCallbackRejected.StaleRun);
                return ActivationCallbackRejected.StaleRun;
            }
            if (!run.TryAdmit())
            {
                logger.RejectedActivationCallback(run.Id, ActivationCallbackRejected.Closed);
                return ActivationCallbackRejected.Closed;
            }

            try
            {
                return await callback();
            }
            finally
            {
                run.Leave();
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    sealed class QueuedCallback<T>
    {
        const int Queued = 0;
        const int Started = 1;
        const int RejectedState = 2;

        readonly TaskCompletionSource<Result<T, ActivationCallbackRejected>> _rejected = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int _state = Queued;

        public Task<Result<T, ActivationCallbackRejected>> Rejected => _rejected.Task;

        public bool TryStart() => Interlocked.CompareExchange(ref _state, Started, Queued) == Queued;

        public void Reject(ActivationCallbackRejected reason)
        {
            if (Interlocked.CompareExchange(ref _state, RejectedState, Queued) == Queued)
            {
                _rejected.TrySetResult(reason);
            }
        }
    }
}
