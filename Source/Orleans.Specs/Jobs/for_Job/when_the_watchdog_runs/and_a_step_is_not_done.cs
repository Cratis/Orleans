// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_the_watchdog_runs;

public class and_a_step_is_not_done : given.a_running_job_with_steps
{
    void Establish() => CountStepsInStorage((JobStepStatus.Running, 1), (JobStepStatus.CompletedWithFailure, 1));

    Task Because() => _job.RunWatchdog();

    [Fact] void should_leave_the_job_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_not_finalize_it() => _job.AllStepsCompletedCount.ShouldEqual(0);
    [Fact] void should_not_change_the_counters() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(0);
}
