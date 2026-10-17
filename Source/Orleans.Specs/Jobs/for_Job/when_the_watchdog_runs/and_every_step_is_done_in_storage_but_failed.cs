// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_the_watchdog_runs;

public class and_every_step_is_done_in_storage_but_failed : given.a_running_job_with_steps
{
    void Establish() => CountStepsInStorage((JobStepStatus.CompletedWithFailure, 1), (JobStepStatus.Failed, 1));

    Task Because() => _job.RunWatchdog();

    [Fact] void should_finalize_the_job_with_failures() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedWithFailures);
    [Fact] void should_count_a_step_that_completed_with_failure_and_one_that_failed_as_failed() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(2);
    [Fact] void should_count_no_success() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(0);
    [Fact] void should_tell_the_job_that_all_steps_are_completed_once() => _job.AllStepsCompletedCount.ShouldEqual(1);
}
