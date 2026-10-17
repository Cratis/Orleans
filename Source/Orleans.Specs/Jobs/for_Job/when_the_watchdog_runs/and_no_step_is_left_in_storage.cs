// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_the_watchdog_runs;

public class and_no_step_is_left_in_storage : given.a_running_job_with_steps
{
    void Establish() => CountStepsInStorage();

    Task Because() => _job.RunWatchdog();

    [Fact] void should_finalize_the_job_as_successful() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedSuccessfully);
    [Fact] void should_count_every_step_as_succeeded() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(2);
}
