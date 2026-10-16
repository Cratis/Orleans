// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_job_was_left_starting_steps : given.a_job_left_running
{
    void Establish() => _job.CurrentState.Status = JobStatus.StartingSteps;
    async Task Because() => _result = await _job.Resume();

    [Fact] void should_restart_the_unfinished_step() => _unfinished.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
    [Fact] void should_report_resumption() => ((ResumeJobSuccess)_result).ShouldEqual(ResumeJobSuccess.Success);
    [Fact] void should_preserve_progress() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(1);
}
