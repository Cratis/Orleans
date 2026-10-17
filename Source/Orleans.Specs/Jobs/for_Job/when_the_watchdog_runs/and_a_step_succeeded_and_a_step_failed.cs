// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_the_watchdog_runs;

public class and_a_step_succeeded_and_a_step_failed : given.a_running_job_with_steps
{
    /// <summary>
    /// The step that succeeded is no longer in storage.
    /// </summary>
    void Establish() => CountStepsInStorage((JobStepStatus.CompletedWithFailure, 1));

    Task Because() => _job.RunWatchdog();

    [Fact] void should_finalize_the_job_with_failures() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedWithFailures);
    [Fact] void should_count_the_step_that_is_no_longer_in_storage_as_succeeded() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_count_the_failure() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(1);
}
