// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_succeeds;

public class for_the_same_attempt_twice : given.a_running_job_with_steps
{
    async Task Because()
    {
        await Succeed(0);
        await Succeed(0);
    }

    [Fact] void should_count_the_success_once() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_handle_the_step_once() => _job.HandledStepResults.ShouldContainOnly(_stepIds[0]);
}
