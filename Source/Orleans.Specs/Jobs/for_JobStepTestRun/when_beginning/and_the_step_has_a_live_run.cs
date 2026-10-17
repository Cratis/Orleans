// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs.for_JobStepTestRun.when_beginning;

public class and_the_step_has_a_live_run : given.a_job_step_with_a_test_run
{
    Exception _error;

    async Task Establish()
    {
        _jobStep.HoldPerformOpen = true;
        _ = _testRun.Perform(new JobStepState());
        await _jobStep.Performed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    void Because() => _error = Catch.Exception(() => _jobStep.BeginTestRun());

    [Fact] void should_refuse_with_a_live_run() => _error.ShouldBeOfExactType<JobStepAlreadyHasALiveRun>();
}
