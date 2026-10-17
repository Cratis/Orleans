// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_the_watchdog_runs;

public class and_the_job_stopped_after_a_resume_start_threw : given.a_job_with_a_throwing_resume_start
{
    async Task Establish()
    {
        _ = await _job.Resume();
        await _job.Stop();
    }

    Task Because() => _job.RunWatchdog();

    [Fact] void should_not_retry_the_step_of_a_stopped_job() => _steps[0].Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
    [Fact] void should_leave_the_job_stopped() => _job.CurrentState.Status.ShouldEqual(JobStatus.Stopped);
}
