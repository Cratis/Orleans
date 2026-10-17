// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_the_watchdog_runs;

public class and_a_throwing_resume_start_was_already_started : given.a_job_with_a_throwing_resume_start
{
    async Task Establish()
    {
        _ = await _job.Resume();
        _steps[0].Setup(step => step.Start(It.IsAny<GrainId>())).ReturnsAsync(StartJobStepError.AlreadyStarted);
    }

    async Task Because()
    {
        await _job.RunWatchdog();
        await _job.RunWatchdog();
        await _job.Stop();
    }

    [Fact] void should_stop_retrying_after_the_step_answers_already_started() => _steps[0].Verify(step => step.Start(It.IsAny<GrainId>()), Times.Exactly(2));
    [Fact] void should_subscribe_the_live_step() => _steps[0].Verify(step => step.OnJobStopped(), Times.Once);
}
