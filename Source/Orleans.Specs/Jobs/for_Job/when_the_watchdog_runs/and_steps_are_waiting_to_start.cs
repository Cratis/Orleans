// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_the_watchdog_runs;

public class and_steps_are_waiting_to_start : given.a_running_job_with_steps
{
    void Establish() => CountStepsInStorage((JobStepStatus.Unknown, 2));

    Task Because() => _job.RunWatchdog();

    [Fact] void should_leave_the_job_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
