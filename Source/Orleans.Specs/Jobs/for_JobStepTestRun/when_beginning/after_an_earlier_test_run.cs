// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs.for_JobStepTestRun.when_beginning;

public class after_an_earlier_test_run : given.a_job_step_with_a_test_run
{
    readonly TaskCompletionSource _releaseLeftover = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task<Result<bool, ActivationCallbackRejected>> _leftover;
    Result<bool, ActivationCallbackRejected> _result;
    JobStepTestRun<JobStepState> _newTestRun;
    bool _ran;

    async Task Establish()
    {
        // The earlier run leaves work behind that tries to use the activation after the run has ended.
        _jobStep.Work = (step, _) =>
        {
            _leftover = Task.Run(async () =>
            {
                await _releaseLeftover.Task;
                return await step.OnActivation(() => Task.FromResult(_ran = true));
            });
            return Task.CompletedTask;
        };
        (await _testRun.Perform(new JobStepState())).IsSuccess.ShouldBeTrue();
        _newTestRun = _jobStep.BeginTestRun();
    }

    async Task Because()
    {
        _releaseLeftover.TrySetResult();
        _result = await _leftover.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_refuse_the_earlier_runs_callback_as_closed() => (_result.TryGetError(out var error) && error == ActivationCallbackRejected.Closed).ShouldBeTrue();
    [Fact] void should_not_run_the_callback() => _ran.ShouldBeFalse();
    [Fact] void should_leave_the_new_run_admitting_callbacks() => _newTestRun.AdmissionOpen.ShouldBeTrue();
}
