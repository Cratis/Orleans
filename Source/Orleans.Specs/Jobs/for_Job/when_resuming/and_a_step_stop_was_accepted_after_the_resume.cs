// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_a_step_stop_was_accepted_after_the_resume : given.a_job_left_running_after_a_resume
{
    void Establish()
    {
        _job.CurrentState.Progress.StoppedSteps = 1;
        StoredJobStep(_unfinishedId).Status = JobStepStatus.Scheduled;
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_not_restart_the_stopped_step() => _stopped.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_restart_the_scheduled_step() => _unfinished.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
    [Fact] void should_preserve_the_accepted_stop() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(1);
}
