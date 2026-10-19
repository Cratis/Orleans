// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.Draining;

/// <summary>
/// Observes a draining step without timing-based waits: when it entered its work, saw cancellation and settled,
/// how often it was activated and how many step state writes reached storage.
/// </summary>
/// <param name="database">The database the cluster's jobs land in.</param>
public sealed class DrainingSignals(string database)
{
    int _writes;
    int _stoppedCallbacks;
    int _activations;
    int _entries;

    /// <summary>
    /// Gets the database the cluster's jobs land in.
    /// </summary>
    public string Database { get; } = database;

    /// <summary>
    /// Gets the jobs manager key the specs run under.
    /// </summary>
    public JobsManagerKey Key { get; } = new("draining-store", "tenant-74");

    /// <summary>
    /// Gets the signal that the step entered its work, carrying the silo it runs on.
    /// </summary>
    public TaskCompletionSource<SiloAddress> Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets or sets a value indicating whether the step succeeds at once when its work is entered again.
    /// </summary>
    public bool SucceedWhenEnteredAgain { get; set; }

    /// <summary>
    /// Gets the signal that the step's work was entered a second time.
    /// </summary>
    public TaskCompletionSource EnteredAgain { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the signal that the job ran its completion callback.
    /// </summary>
    public TaskCompletionSource JobCompleted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the signal that the step's work observed cancellation.
    /// </summary>
    public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets or sets a value indicating whether the step holds on to its work after observing cancellation until
    /// <see cref="ReleaseWork"/> is completed.
    /// </summary>
    public bool HoldAfterCancellation { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the step holds its final checkpoint callback open, on its activation,
    /// until <see cref="ReleaseWork"/> is completed.
    /// </summary>
    public bool HoldInFinalCheckpoint { get; set; }

    /// <summary>
    /// Gets the signal that the step is inside its final checkpoint callback.
    /// </summary>
    public TaskCompletionSource InFinalCheckpoint { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the signal that a step activation deactivated.
    /// </summary>
    public TaskCompletionSource Deactivated { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the gate a step holding on to its work after cancellation waits on.
    /// </summary>
    public TaskCompletionSource ReleaseWork { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the outcome of the checkpoint the step writes once it has been cancelled.
    /// </summary>
    public TaskCompletionSource<Result<ActivationCallbackRejected>> FinalCheckpoint { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the signal that a write recording the step as stopped reached storage.
    /// </summary>
    public TaskCompletionSource StopPersisted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the signal that the step's background work has returned.
    /// </summary>
    public TaskCompletionSource WorkerSettled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the signal that the job ran its stopped callback.
    /// </summary>
    public TaskCompletionSource JobStopped { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets or sets whether the job's stopped callback throws the first time it runs.
    /// </summary>
    public bool FailFirstStop { get; set; }

    /// <summary>
    /// Gets the number of times the job's stopped callback ran.
    /// </summary>
    public int StoppedCallbacks => Volatile.Read(ref _stoppedCallbacks);

    /// <summary>
    /// Records that the job's stopped callback ran.
    /// </summary>
    /// <returns>True if the callback should fail this time.</returns>
    public bool StoppedCallbackRan() => Interlocked.Increment(ref _stoppedCallbacks) == 1 && FailFirstStop;

    /// <summary>
    /// Gets the signal that the job recorded the outcome of its step.
    /// </summary>
    public TaskCompletionSource StepOutcomeRecorded { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the number of step state writes that reached storage.
    /// </summary>
    public int Writes => Volatile.Read(ref _writes);

    /// <summary>
    /// Gets the number of times a step was activated.
    /// </summary>
    public int Activations => Volatile.Read(ref _activations);

    /// <summary>
    /// Records a step state write.
    /// </summary>
    public void Wrote() => Interlocked.Increment(ref _writes);

    /// <summary>
    /// Gets the number of times the step's work was entered.
    /// </summary>
    public int Entries => Volatile.Read(ref _entries);

    /// <summary>
    /// Records that the step's work was entered.
    /// </summary>
    /// <returns>True if the work was entered before.</returns>
    public bool Enter()
    {
        if (Interlocked.Increment(ref _entries) == 1)
        {
            return false;
        }
        EnteredAgain.TrySetResult();
        return true;
    }

    /// <summary>
    /// Records a step activation.
    /// </summary>
    public void Activated() => Interlocked.Increment(ref _activations);
}
