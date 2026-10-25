// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_the_watchdog_runs;

public class and_a_job_is_stopped_after_its_resume_hook_failed : for_Job.given.an_interrupted_job_rejecting_resume
{
    async Task Establish()
    {
        CountStepsInStorage((JobStepStatus.Running, 1));
        _ = await _job.Resume();
        _job.CurrentState.Status = JobStatus.Stopped;
        _job.BeforeResuming = () => Task.CompletedTask;
    }

    Task Because() => _job.RunWatchdog();

    [Fact] void should_not_run_the_hook_again() => _job.BeforeResumingCount.ShouldEqual(1);
    [Fact] void should_leave_the_job_stopped() => _job.CurrentState.Status.ShouldEqual(JobStatus.Stopped);
}
