// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_a_fresh_activation_reads_the_compensated_snapshot : given.a_stopped_job_rejecting_resume
{
    async Task Establish()
    {
        await _job.Resume();
        await ActivateFrom(_job.WrittenStates[^1]);
        _step.Setup(step => step.Start(It.IsAny<GrainId>()))
            .Callback(() => _job.BeforeResumingCount.ShouldEqual(1))
            .ReturnsAsync(Result<StartJobStepError>.Success());
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_resume_successfully() => ((ResumeJobSuccess)_result).ShouldEqual(ResumeJobSuccess.Success);
    [Fact] void should_run_the_hook_before_dispatch() => _job.BeforeResumingCount.ShouldEqual(1);
    [Fact] void should_start_the_stopped_step() => _step.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
    [Fact] void should_reset_stopped_progress() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(0);
}
