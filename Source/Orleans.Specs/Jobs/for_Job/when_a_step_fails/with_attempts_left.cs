// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_fails;

public class with_attempts_left : given.a_running_job_with_steps
{
    protected override JobsOptions CreateOptions() => new() { MaxStepAttempts = 3, StepRetryBackoff = TimeSpan.FromHours(1) };

    Task Because() => Fail(0);

    [Fact] void should_not_count_the_failure_yet() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(0);
    [Fact] void should_not_handle_the_step_as_done() => _job.HandledStepResults.ShouldBeEmpty();
    [Fact] void should_leave_the_job_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
