// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_running_in_stages;

public class and_the_last_stage_fails : given.a_job_with_three_stages
{
    async Task Because()
    {
        await _job.Start(new());
        await Succeed(_first);
        await Succeed(_secondA);
        await Succeed(_secondB);
        await Fail(_third);
    }

    [Fact] void should_not_count_any_step_as_unreachable() => _job.CurrentState.Progress.UnreachableSteps.ShouldEqual(0);
    [Fact] void should_complete_the_job_with_failures() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedWithFailures);
}
