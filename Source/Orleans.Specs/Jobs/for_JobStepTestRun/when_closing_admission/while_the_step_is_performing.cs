// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs.for_JobStepTestRun.when_closing_admission;

public class while_the_step_is_performing : given.a_job_step_with_a_test_run
{
    Task<Result<bool, ActivationCallbackRejected>> _callback;
    Task<Catch<JobStepResult>> _performing;
    Result<bool, ActivationCallbackRejected> _result;
    Catch<JobStepResult> _performed;
    bool _ran;

    async Task Establish()
    {
        _callback = WorkInvokesOnActivationAfterProceeding(() => Task.FromResult(_ran = true));
        _performing = _testRun.Perform(new JobStepState());
        await _jobStep.Performed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        _testRun.CloseAdmission();
        _proceed.TrySetResult();
        _result = await _callback.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _performed = await _performing.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_refuse_the_callback_as_closed() => _result.AsT1.ShouldEqual(ActivationCallbackRejected.Closed);
    [Fact] void should_not_run_the_callback() => _ran.ShouldBeFalse();
    [Fact] void should_not_cancel_the_work() => _testRun.Cancellation.IsCancellationRequested.ShouldBeFalse();
    [Fact] void should_return_what_the_step_returned() => _performed.IsSuccess.ShouldBeTrue();
}
