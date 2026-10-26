// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_running_in_stages;

public class and_the_request_is_missing_without_registered_admission : given.a_job_with_three_stages
{
    async Task Establish()
    {
        await _job.Start(new());
        _job.CurrentState.Request = null!;
    }

    async Task Because() => await Succeed(_first);

    [Fact] void should_have_started_the_first_stage() => ShouldHaveStarted(_firstStep);
    [Fact] void should_start_the_first_step_in_the_next_stage() => ShouldHaveStarted(_secondStepA);
    [Fact] void should_start_the_second_step_in_the_next_stage() => ShouldHaveStarted(_secondStepB);
    [Fact] void should_not_start_the_last_stage() => ShouldNotHaveStarted(_thirdStep);
    [Fact] void should_stay_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_not_complete() => _job.AllStepsCompletedCount.ShouldEqual(0);
    [Fact] void should_not_run_the_stopped_hook() => _job.StoppedCount.ShouldEqual(0);
}
