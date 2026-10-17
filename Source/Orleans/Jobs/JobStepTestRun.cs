// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Represents a step run begun for a specification with <see cref="JobStepTestRunExtensions.BeginTestRun{TRequest, TResult, TState}"/>.
/// </summary>
/// <remarks>
/// <para>
/// It gives a step under specification the run its work has in a silo: <c language="csharp">TryInvokeOnActivation</c> admits
/// callbacks and runs them, serialized, on <see cref="Activation"/>, <see cref="Stop"/> cancels the work while
/// admission stays open, and <see cref="CloseAdmission"/> makes the step refuse callbacks that have not started.
/// </para>
/// <para>
/// It does not record or deliver the step's outcome, write the step's status or notify a job. <see cref="Perform"/>
/// returns what the step's work returned and nothing else happens to it. Specify those behaviors by starting and
/// stopping the step through its grain interface.
/// </para>
/// </remarks>
/// <typeparam name="TState">Type of state for the step.</typeparam>
public sealed class JobStepTestRun<TState>
    where TState : JobStepState
{
    readonly JobStepRun _run;
    readonly Func<JobStepRun, TState, Task<Catch<JobStepResult>>> _perform;
    int _performed;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobStepTestRun{TState}"/> class.
    /// </summary>
    /// <param name="run">The <see cref="JobStepRun"/> the step is running under.</param>
    /// <param name="perform">Performs the step under the run.</param>
    internal JobStepTestRun(JobStepRun run, Func<JobStepRun, TState, Task<Catch<JobStepResult>>> perform)
    {
        _run = run;
        _perform = perform;
    }

    /// <summary>
    /// Gets the scheduler standing in for the step's activation. Callbacks run on it; nothing else does.
    /// </summary>
    public TaskScheduler Activation => _run.Scheduler;

    /// <summary>
    /// Gets a value indicating whether the calling code is running on <see cref="Activation"/>.
    /// </summary>
    public bool IsOnActivation => TaskScheduler.Current == Activation;

    /// <summary>
    /// Gets the token the step's work is cancelled through. It is the token <see cref="Perform"/> gives the step.
    /// </summary>
    public CancellationToken Cancellation => _run.Cancellation;

    /// <summary>
    /// Gets a value indicating whether the run still admits activation callbacks.
    /// </summary>
    public bool AdmissionOpen => _run.AdmissionOpen;

    /// <summary>
    /// Gets a task that completes when no admitted callback is running.
    /// </summary>
    public Task CallbacksSettled => _run.CallbacksSettled;

    /// <summary>
    /// Performs the step's work under the run, off the activation, with the given state and <see cref="Cancellation"/>.
    /// </summary>
    /// <param name="currentState">The state to give the step's work.</param>
    /// <returns>What the step's work returned. Admission is closed once it has returned, as a drain does.</returns>
    /// <exception cref="JobStepAlreadyHasALiveRun">Thrown when this test run has already performed its step.</exception>
    /// <remarks>
    /// The outcome is neither recorded nor delivered to a job.
    /// </remarks>
    public Task<Catch<JobStepResult>> Perform(TState currentState)
    {
        if (Interlocked.Exchange(ref _performed, 1) != 0)
        {
            throw new JobStepAlreadyHasALiveRun("This test run has already performed its step. Begin a new test run once the earlier one has settled.");
        }

        return _perform(_run, currentState);
    }

    /// <summary>
    /// Stops the run as an ordinary stop does: it cancels the work and keeps admission open, so the work can still
    /// write a final checkpoint through <c language="csharp">TryInvokeOnActivation</c>.
    /// </summary>
    public void Stop() => _run.BeginDrain(JobStepRunPhase.Stopping);

    /// <summary>
    /// Closes admission as a stop past its deadline or a drained deactivation does: callbacks that have not
    /// started, and any posted afterwards, are refused as closed. It does not cancel the work.
    /// </summary>
    public void CloseAdmission() => _run.CloseAdmission();
}
