// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming_if_running;

public class and_the_resume_hook_throws : for_Job.given.an_interrupted_job_rejecting_resume
{
    async Task Because() => _result = await _job.ResumeIfRunning();

    [Fact] void should_report_failure() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_preserve_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_not_start_a_step() => _unfinished.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
}
