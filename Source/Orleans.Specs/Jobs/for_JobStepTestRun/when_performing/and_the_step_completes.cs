// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs.for_JobStepTestRun.when_performing;

public class and_the_step_completes : given.a_job_step_with_a_test_run
{
    Catch<JobStepResult> _result;
    JobStepState _state;

    async Task Because()
    {
        _state = new();
        _result = await _testRun.Perform(_state).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_return_what_the_step_returned() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_give_the_step_the_state() => _jobStep.ReceivedState.ShouldBeSame(_state);
    [Fact] void should_give_the_step_the_runs_cancellation() => _jobStep.PerformToken.ShouldEqual(_testRun.Cancellation);
    [Fact] void should_close_admission() => _testRun.AdmissionOpen.ShouldBeFalse();
}
