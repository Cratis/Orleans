// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_only_stopped_steps_remain : given.a_job_left_running
{
    void Establish() => StoredJobStep(_unfinishedId).Status = JobStepStatus.Stopped;
    async Task Because() => _result = await _job.Resume();

    [Fact] void should_not_revive_stopped_work() => _unfinished.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_not_claim_recovery_succeeded() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_preserve_stopped_progress() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(1);
    [Fact] void should_preserve_successful_progress() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(1);
}
