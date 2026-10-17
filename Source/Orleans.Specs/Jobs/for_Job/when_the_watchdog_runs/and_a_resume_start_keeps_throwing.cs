// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_the_watchdog_runs;

public class and_a_resume_start_keeps_throwing : given.a_job_with_a_throwing_resume_start
{
    async Task Establish() => _ = await _job.Resume();

    async Task Because()
    {
        await _job.RunWatchdog();
        await _job.RunWatchdog();
    }

    [Fact] void should_try_the_throwing_start_again_on_each_tick() => _steps[0].Verify(step => step.Start(It.IsAny<GrainId>()), Times.Exactly(3));
    [Fact] void should_not_restart_the_other_steps() => _steps.Skip(1).ToList().ForEach(step => step.Verify(call => call.Start(It.IsAny<GrainId>()), Times.Once));
    [Fact] void should_leave_the_job_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
