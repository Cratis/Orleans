// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming_if_running;

public class and_the_job_is_running : for_Job.given.an_interrupted_job_rejecting_resume
{
    void Establish() => _job.BeforeResuming = () => Task.CompletedTask;

    async Task Because() => _result = await _job.ResumeIfRunning();

    [Fact] void should_resume_the_job() => ((ResumeJobSuccess)_result).ShouldEqual(ResumeJobSuccess.Success);
    [Fact] void should_be_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_run_the_hook_once() => _job.BeforeResumingCount.ShouldEqual(1);
    [Fact] void should_start_the_unfinished_step_once() => _unfinished.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
}
