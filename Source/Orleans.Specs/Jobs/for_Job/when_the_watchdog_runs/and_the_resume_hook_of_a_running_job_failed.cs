// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_the_watchdog_runs;

public class and_the_resume_hook_of_a_running_job_failed : for_Job.given.an_interrupted_job_rejecting_resume
{
    async Task Establish()
    {
        CountStepsInStorage((JobStepStatus.Running, 1));
        _ = await _job.Resume();
        _job.BeforeResuming = () => Task.CompletedTask;
    }

    async Task Because()
    {
        await _job.RunWatchdog();
        await _job.RunWatchdog();
    }

    [Fact] void should_run_the_hook_again_once() => _job.BeforeResumingCount.ShouldEqual(2);
    [Fact] void should_restart_the_unfinished_step_once() => _unfinished.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
    [Fact] void should_leave_the_job_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
