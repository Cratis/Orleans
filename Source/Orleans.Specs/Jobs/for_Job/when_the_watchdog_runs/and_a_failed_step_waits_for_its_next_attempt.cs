// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_the_watchdog_runs;

public class and_a_failed_step_waits_for_its_next_attempt : given.a_running_job_with_steps
{
    protected override JobsOptions CreateOptions() => new() { MaxStepAttempts = 2, StepRetryBackoff = TimeSpan.FromHours(1) };

    async Task Establish()
    {
        await Fail(0);
        CountStepsInStorage((JobStepStatus.CompletedWithFailure, 1), (JobStepStatus.Running, 1));
        await Succeed(1);
        CountStepsInStorage((JobStepStatus.CompletedWithFailure, 1));
    }

    Task Because() => _job.RunWatchdog();

    [Fact] void should_not_finalize_a_job_that_is_still_waiting_for_an_attempt() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
