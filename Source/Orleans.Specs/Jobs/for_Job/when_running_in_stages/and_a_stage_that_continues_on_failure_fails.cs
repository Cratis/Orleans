// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_running_in_stages;

public class and_a_stage_that_continues_on_failure_fails : given.a_job_with_three_stages
{
    void Establish() => _job.StagesContinuingOnFailure.Add(10);

    async Task Because()
    {
        await _job.Start(new());
        await Succeed(_first);
        await Fail(_secondA);
        await Succeed(_secondB);
        await Succeed(_third);
    }

    [Fact] void should_start_the_step_in_the_next_stage() => ShouldHaveStarted(_thirdStep);
    [Fact] void should_not_count_any_step_as_unreachable() => _job.CurrentState.Progress.UnreachableSteps.ShouldEqual(0);
    [Fact] void should_complete_the_job_with_failures() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedWithFailures);
}
