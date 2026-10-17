// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs.for_JobStep.given;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs.for_JobStepTestRun.given;

/// <summary>
/// A hosted job step that has begun a test run, whose work is held open until a specification releases it.
/// </summary>
public class a_job_step_with_a_test_run : a_job_step_left_scheduled_by_a_dead_process
{
    protected JobStepTestRun<JobStepState> _testRun;
    protected TaskCompletionSource _proceed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish() => _testRun = _jobStep.BeginTestRun();

    /// <summary>
    /// Makes the step's work wait until <see cref="_proceed"/> is released and then run a callback on the activation.
    /// </summary>
    /// <param name="callback">The callback to run.</param>
    /// <returns>The task completing with the callback's result.</returns>
    protected Task<Result<bool, ActivationCallbackRejected>> WorkInvokesOnActivationAfterProceeding(Func<Task<bool>> callback)
    {
        var invoked = new TaskCompletionSource<Result<bool, ActivationCallbackRejected>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _jobStep.Work = async (step, _) =>
        {
            await _proceed.Task;
            invoked.TrySetResult(await step.OnActivation(callback));
        };
        return invoked.Task;
    }

    void Destroy()
    {
        _proceed.TrySetResult();
        _jobStep.Gate.TrySetResult();
    }
}
