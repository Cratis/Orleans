// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_running_in_stages;

public class and_a_stage_is_still_running_when_one_of_its_steps_fails : given.a_job_with_three_stages
{
    async Task Because()
    {
        await _job.Start(new());
        await Succeed(_first);
        await Fail(_secondA);
    }

    [Fact] void should_not_count_any_step_as_unreachable_yet() => _job.CurrentState.Progress.UnreachableSteps.ShouldEqual(0);
    [Fact] void should_still_be_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
