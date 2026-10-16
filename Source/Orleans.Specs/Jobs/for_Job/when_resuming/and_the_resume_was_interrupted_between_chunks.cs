// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_resume_was_interrupted_between_chunks : given.a_job_left_running_after_a_resume
{
    protected override JobsOptions CreateOptions() => new() { MaxConcurrentStepStarts = 1 };

    void Establish() => StoredJobStep(_unfinishedId).Status = JobStepStatus.Scheduled;
    async Task Because() => _result = await _job.Resume();

    [Fact] void should_restart_the_scheduled_step() => _unfinished.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
    [Fact] void should_restart_the_stopped_step() => _stopped.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
    [Fact] void should_not_restart_the_completed_step() => _completed.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_report_resumption() => ((ResumeJobSuccess)_result).ShouldEqual(ResumeJobSuccess.Success);
}
