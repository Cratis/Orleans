// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_the_watchdog_runs;

public class and_a_resume_start_succeeds_on_retry : given.a_job_with_a_throwing_resume_start
{
    async Task Establish()
    {
        _ = await _job.Resume();
        _steps[0].Setup(step => step.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Success());
    }

    async Task Because()
    {
        await _job.RunWatchdog();
        await _job.RunWatchdog();
        await _job.Stop();
    }

    [Fact] void should_retry_the_throwing_start_only_until_it_succeeds() => _steps[0].Verify(step => step.Start(It.IsAny<GrainId>()), Times.Exactly(2));
    [Fact] void should_subscribe_the_step_after_it_starts() => _steps[0].Verify(step => step.OnJobStopped(), Times.Once);
}
