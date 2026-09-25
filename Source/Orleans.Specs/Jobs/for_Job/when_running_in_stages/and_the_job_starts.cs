// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_running_in_stages;

public class and_the_job_starts : given.a_job_with_three_stages
{
    async Task Because() => await _job.Start(new());

    [Fact] void should_start_the_step_in_the_first_stage() => ShouldHaveStarted(_firstStep);
    [Fact] void should_not_start_the_first_step_in_the_second_stage() => ShouldNotHaveStarted(_secondStepA);
    [Fact] void should_not_start_the_second_step_in_the_second_stage() => ShouldNotHaveStarted(_secondStepB);
    [Fact] void should_not_start_the_step_in_the_third_stage() => ShouldNotHaveStarted(_thirdStep);
    [Fact] void should_prepare_every_step_up_front() => _thirdStep.Verify(_ => _.Prepare(It.IsAny<object>(), 20), Times.Once);
    [Fact] void should_be_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_record_the_stages_in_order() => _job.CurrentState.Progress.Stages.Select(_ => _.Stage.Value).ShouldContainOnly(0, 10, 20);
    [Fact] void should_count_the_steps_of_the_second_stage() => _job.CurrentState.Progress.Stages[1].TotalSteps.ShouldEqual(2);
}
