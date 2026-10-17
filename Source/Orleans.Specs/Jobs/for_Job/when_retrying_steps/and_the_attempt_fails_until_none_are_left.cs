// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_retrying_steps;

public class and_the_attempt_fails_until_none_are_left : given.a_running_job_with_steps
{
    protected override JobsOptions CreateOptions() => new() { MaxStepAttempts = 3, StepRetryBackoff = TimeSpan.Zero };

    async Task Because()
    {
        await Fail(0, attempt: 1);
        await _job.RetryDueSteps();
        await Fail(0, attempt: 2);
        await _job.RetryDueSteps();
        await Fail(0, attempt: 3);
    }

    [Fact] void should_count_one_failure() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(1);
    [Fact] void should_handle_the_step_once() => _job.HandledStepResults.ShouldContainOnly(_stepIds[0]);
}
