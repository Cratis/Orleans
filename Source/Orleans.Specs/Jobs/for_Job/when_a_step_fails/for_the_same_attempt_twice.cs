// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_fails;

public class for_the_same_attempt_twice : given.a_running_job_with_steps
{
    async Task Because()
    {
        await Fail(0);
        await Fail(0);
    }

    [Fact] void should_count_the_failure_once() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(1);
    [Fact] void should_handle_the_step_once() => _job.HandledStepResults.ShouldContainOnly(_stepIds[0]);
}
