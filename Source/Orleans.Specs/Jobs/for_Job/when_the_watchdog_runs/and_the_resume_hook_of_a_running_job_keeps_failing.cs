// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_the_watchdog_runs;

public class and_the_resume_hook_of_a_running_job_keeps_failing : for_Job.given.an_interrupted_job_rejecting_resume
{
    async Task Establish()
    {
        CountStepsInStorage((JobStepStatus.Running, 1));
        _ = await _job.Resume();
    }

    async Task Because()
    {
        for (var tick = 0; tick < 7; tick++)
        {
            await _job.RunWatchdog();
        }
    }

    [Fact] void should_back_off_between_retries() => _job.BeforeResumingCount.ShouldEqual(4);
    [Fact] void should_not_start_a_step() => _unfinished.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_leave_the_job_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
