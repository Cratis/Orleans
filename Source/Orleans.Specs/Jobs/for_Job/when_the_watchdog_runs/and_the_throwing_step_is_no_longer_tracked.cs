// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_the_watchdog_runs;

public class and_the_throwing_step_is_no_longer_tracked : given.a_job_with_a_throwing_resume_start
{
    async Task Establish()
    {
        _ = await _job.Resume();
        await Succeed(0);
    }

    Task Because() => _job.RunWatchdog();

    [Fact] void should_not_restart_a_step_whose_outcome_was_accepted() => _steps[0].Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
}
