// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_the_watchdog_runs;

public class and_a_throwing_resume_start_is_later_refused : given.a_job_with_a_throwing_resume_start
{
    async Task Establish()
    {
        _ = await _job.Resume();
        _steps[0].Setup(step => step.Start(It.IsAny<GrainId>())).ReturnsAsync(StartJobStepError.NotPrepared);
    }

    async Task Because()
    {
        await _job.RunWatchdog();
        await _job.RunWatchdog();
    }

    [Fact] void should_stop_retrying_after_an_explicit_refusal() => _steps[0].Verify(step => step.Start(It.IsAny<GrainId>()), Times.Exactly(2));
}
