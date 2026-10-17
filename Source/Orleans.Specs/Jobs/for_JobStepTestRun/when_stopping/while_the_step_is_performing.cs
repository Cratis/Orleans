// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs.for_JobStepTestRun.when_stopping;

public class while_the_step_is_performing : given.a_job_step_with_a_test_run
{
    Task<Result<bool, ActivationCallbackRejected>> _callback;
    Task<Catch<JobStepResult>> _performing;
    Result<bool, ActivationCallbackRejected> _result;
    bool _ran;

    async Task Establish()
    {
        _callback = WorkInvokesOnActivationAfterProceeding(() => Task.FromResult(_ran = true));
        _jobStep.HoldPerformOpen = true;
        _performing = _testRun.Perform(new JobStepState());
        await _jobStep.Performed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        _testRun.Stop();
        await _jobStep.PerformCancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _proceed.TrySetResult();
        _result = await _callback.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_cancel_the_work() => _testRun.Cancellation.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_keep_admission_open() => _testRun.AdmissionOpen.ShouldBeTrue();
    [Fact] void should_still_admit_a_callback() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_run_the_callback() => _ran.ShouldBeTrue();
}
