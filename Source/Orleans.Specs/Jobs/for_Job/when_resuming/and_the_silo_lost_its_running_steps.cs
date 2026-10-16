// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_silo_lost_its_running_steps : given.a_job_left_running
{
    async Task Because() => _result = await _job.Resume();

    [Fact] void should_restart_the_unfinished_step() => _unfinished.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
    [Fact] void should_not_restart_the_completed_step() => _completed.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_not_restart_the_stopped_step() => _stopped.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_report_resumption() => ((ResumeJobSuccess)_result).ShouldEqual(ResumeJobSuccess.Success);
    [Fact] void should_preserve_successful_progress() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_preserve_stopped_progress() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(1);
    [Fact] void should_not_write_a_status_change() => _job.CurrentState.StatusChanges.Count.ShouldEqual(1);
}
