// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_job_was_left_starting_steps : given.a_job_with_a_durable_plan
{
    async Task Establish()
    {
        await _job.Start(new());
        await ActivateFrom(JobStatus.StartingSteps);
        _first.Setup(step => step.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Failed(StartJobStepError.AlreadyStarted));
        _second.Setup(step => step.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Failed(StartJobStepError.AlreadyStarted));
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_confirm_the_unfinished_step() => _first.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
    [Fact] void should_report_recovery_not_stale_running() => ((ResumeJobSuccess)_result).ShouldEqual(ResumeJobSuccess.Success);
    [Fact] void should_finish_startup() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_signal_incomplete_initialization() => _job.DispatchContexts.Single().InitializationMayBeIncomplete.ShouldBeTrue();
}
