// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_running_in_stages;

public class and_the_first_stage_succeeds : given.a_job_with_three_stages
{
    async Task Because()
    {
        await _job.Start(new());
        await Succeed(_first);
    }

    [Fact] void should_start_the_first_step_in_the_second_stage() => ShouldHaveStarted(_secondStepA);
    [Fact] void should_start_the_second_step_in_the_second_stage() => ShouldHaveStarted(_secondStepB);
    [Fact] void should_not_start_the_step_in_the_third_stage() => ShouldNotHaveStarted(_thirdStep);
    [Fact] void should_still_be_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
