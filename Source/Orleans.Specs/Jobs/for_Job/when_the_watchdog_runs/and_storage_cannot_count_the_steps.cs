// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_the_watchdog_runs;

public class and_storage_cannot_count_the_steps : given.a_running_job_with_steps
{
    void Establish() => StorageCannotCountSteps();

    Task Because() => _job.RunWatchdog();

    [Fact] void should_leave_the_job_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_not_finalize_it() => _job.AllStepsCompletedCount.ShouldEqual(0);
}
