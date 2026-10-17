// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_retrying_steps;

public class and_the_job_is_stopped_during_the_backoff : given.a_running_job_with_steps
{
    protected override JobsOptions CreateOptions() => new() { MaxStepAttempts = 3, StepRetryBackoff = TimeSpan.FromHours(1) };

    async Task Because()
    {
        await Fail(0);
        await _job.Stop();
    }

    [Fact] void should_count_the_failure_that_was_waiting_for_an_attempt() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(1);
}
