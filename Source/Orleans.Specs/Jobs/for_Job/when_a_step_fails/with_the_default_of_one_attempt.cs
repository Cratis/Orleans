// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_fails;

public class with_the_default_of_one_attempt : given.a_running_job_with_steps
{
    Task Because() => Fail(0);

    [Fact] void should_count_the_failure() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(1);
    [Fact] void should_handle_the_step() => _job.HandledStepResults.ShouldContainOnly(_stepIds[0]);
}
