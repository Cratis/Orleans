// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_the_watchdog_runs;

public class and_a_resume_start_is_refused : given.a_job_with_a_throwing_resume_start
{
    async Task Establish()
    {
        _steps[0].Setup(step => step.Start(It.IsAny<GrainId>())).ReturnsAsync(StartJobStepError.NotPrepared);
        _ = await _job.Resume();
    }

    Task Because() => _job.RunWatchdog();

    [Fact] void should_not_retry_an_explicit_start_refusal() => _steps[0].Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
}
