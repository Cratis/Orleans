// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_the_watchdog_runs;

public class and_the_stopped_jobs_resume_hook_failed : for_Job.given.a_stopped_job_rejecting_resume
{
    async Task Establish()
    {
        _ = await _job.Resume();
        _job.BeforeResuming = () => Task.CompletedTask;
    }

    Task Because() => _job.RunWatchdog();

    [Fact] void should_not_run_the_hook_again() => _job.BeforeResumingCount.ShouldEqual(1);
    [Fact] void should_leave_the_job_stopped() => _job.CurrentState.Status.ShouldEqual(JobStatus.Stopped);
}
