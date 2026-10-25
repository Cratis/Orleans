// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_a_stage_completes_while_the_resume_hook_is_owed;

public class and_the_job_is_stopped_then_resumed : given.a_job_with_a_durable_plan
{
    JobStatus _stopped;

    async Task Establish()
    {
        await _job.Start(new());
        await ActivateFrom(JobStatus.Running);
        _job.ShouldBeResumable = true;
        (await _job.OnStepSucceeded(_firstId, JobStepResult.Succeeded())).IsSuccess.ShouldBeTrue();
        (await _job.OnStepSucceeded(_secondId, JobStepResult.Succeeded())).IsSuccess.ShouldBeTrue();
    }

    async Task Because()
    {
        _ = await _job.Stop();
        _stopped = _job.CurrentState.Status;
        _result = await _job.Resume();
    }

    [Fact] void should_stop() => _stopped.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_resume() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_start_the_next_stage_once() => _later.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
    [Fact] void should_run_the_resume_hook_once() => _job.BeforeResumingCount.ShouldEqual(1);
}
