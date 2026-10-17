// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_a_fresh_activation_reads_the_failed_resumes_running_snapshot : given.a_stopped_job_rejecting_resume
{
    async Task Establish()
    {
        await _job.Resume();

        // A crash after the durable Running write, before the hook's compensation is durable.
        await ActivateFrom(_job.WrittenStates[0]);
        _step.Setup(step => step.Start(It.IsAny<GrainId>()))
            .Callback(() => _job.BeforeResumingCount.ShouldEqual(1))
            .ReturnsAsync(Result<StartJobStepError>.Success());
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_recover_the_interrupted_resume() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_run_the_hook_before_dispatch() => _job.BeforeResumingCount.ShouldEqual(1);
    [Fact] void should_restart_the_stopped_step_from_the_earlier_execution() => _step.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
    [Fact] void should_preserve_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
