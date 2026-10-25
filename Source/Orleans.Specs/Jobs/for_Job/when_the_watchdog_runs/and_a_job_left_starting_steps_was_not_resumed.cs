// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_the_watchdog_runs;

public class and_a_job_left_starting_steps_was_not_resumed : given.a_job_with_a_durable_plan
{
    async Task Establish()
    {
        await _job.Start(new());
        await ActivateFrom(JobStatus.StartingSteps);
        _first.Setup(step => step.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Failed(StartJobStepError.AlreadyStarted));
        _second.Setup(step => step.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Failed(StartJobStepError.AlreadyStarted));
    }

    Task Because() => _job.RunWatchdog();

    [Fact] void should_run_the_resume_hook() => _job.BeforeResumingCount.ShouldEqual(1);
    [Fact] void should_finish_startup() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
