// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_job_was_never_stopped : given.a_job_left_running_after_a_resume
{
    void Establish() => _job.CurrentState.StatusChanges.RemoveAt(2);
    async Task Because() => _result = await _job.Resume();

    [Fact] void should_not_restart_the_stopped_step() => _stopped.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_not_restart_the_other_stopped_step() => _unfinished.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_not_claim_recovery_succeeded() => _result.IsSuccess.ShouldBeFalse();
}
