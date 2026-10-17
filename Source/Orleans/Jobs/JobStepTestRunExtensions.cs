// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Extension methods for running a <see cref="JobStep{TRequest, TResult, TState}"/> under a step run in a specification.
/// </summary>
public static class JobStepTestRunExtensions
{
    /// <summary>
    /// Begins a run for a step hosted in a specification, so its work has the run it has in a silo.
    /// </summary>
    /// <typeparam name="TRequest">Type of request for the step.</typeparam>
    /// <typeparam name="TResult">Type of result for the step.</typeparam>
    /// <typeparam name="TState">Type of state for the step.</typeparam>
    /// <param name="step">The step, hosted for example in an <c language="csharp">Orleans.TestKit</c> silo.</param>
    /// <param name="activation">The scheduler that stands in for the step's activation. Defaults to a new exclusive scheduler.</param>
    /// <returns>The <see cref="JobStepTestRun{TState}"/>.</returns>
    /// <exception cref="JobStepAlreadyHasALiveRun">Thrown when the step's earlier run has not settled.</exception>
    /// <remarks>
    /// The run does not record or deliver outcomes: use it to specify a step's own work, not its reporting to a job.
    /// A callback from an earlier test run of the same step is refused as closed.
    /// Use it in specifications only: it changes the step's run outside the activation, so never call it on a step a silo is running.
    /// </remarks>
    public static JobStepTestRun<TState> BeginTestRun<TRequest, TResult, TState>(
        this JobStep<TRequest, TResult, TState> step,
        TaskScheduler? activation = null)
        where TState : JobStepState
    {
        var run = step.BeginTestRun(activation ?? new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler);
        return new(run, step.PerformInRun);
    }
}
