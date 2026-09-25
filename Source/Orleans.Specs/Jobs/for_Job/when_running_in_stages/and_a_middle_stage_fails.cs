// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_running_in_stages;

public class and_a_middle_stage_fails : given.a_job_with_three_stages
{
    async Task Because()
    {
        await _job.Start(new());
        await Succeed(_first);
        await Fail(_secondA);
        await Succeed(_secondB);
    }

    [Fact] void should_not_start_the_step_in_the_third_stage() => ShouldNotHaveStarted(_thirdStep);
    [Fact] void should_count_the_step_behind_it_as_unreachable() => _job.CurrentState.Progress.UnreachableSteps.ShouldEqual(1);
    [Fact] void should_count_the_failed_step_in_its_stage() => _job.CurrentState.Progress.Stages[1].FailedSteps.ShouldEqual(1);
    [Fact] void should_complete_the_job_with_failures() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedWithFailures);
}
