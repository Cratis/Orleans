// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_running_in_stages;

public class and_the_first_stage_fails : given.a_job_with_three_stages
{
    async Task Because()
    {
        await _job.Start(new());
        await Fail(_first);
    }

    [Fact] void should_not_start_the_first_step_in_the_second_stage() => ShouldNotHaveStarted(_secondStepA);
    [Fact] void should_not_start_the_second_step_in_the_second_stage() => ShouldNotHaveStarted(_secondStepB);
    [Fact] void should_not_start_the_step_in_the_third_stage() => ShouldNotHaveStarted(_thirdStep);
    [Fact] void should_record_the_steps_behind_it_as_unreachable() => StoredJobStepsWith(JobStepStatus.Unreachable).ShouldEqual(3);
    [Fact] void should_record_a_step_behind_it_as_unreachable() => _thirdStep.Verify(_ => _.ReportStatusChange(JobStepStatus.Unreachable), Times.Once);
    [Fact] void should_count_the_steps_behind_it_as_unreachable() => _job.CurrentState.Progress.UnreachableSteps.ShouldEqual(3);
    [Fact] void should_account_for_every_step() => _job.CurrentState.Progress.IsCompleted.ShouldBeTrue();
    [Fact] void should_complete_the_job_with_failures() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedWithFailures);
}
