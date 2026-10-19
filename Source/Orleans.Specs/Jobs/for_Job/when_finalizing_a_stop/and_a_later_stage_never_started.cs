// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Stages;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_finalizing_a_stop;

public class and_a_later_stage_never_started : given.a_job_whose_silo_went_away_while_stopping
{
    JobStepId _laterStepId;
    Mock<given.ISomeJobStep> _laterStep;

    void Establish()
    {
        _laterStepId = Guid.Parse("7d7d7d7d-0000-0000-0000-000000000002");
        _laterStep = AddNotifiableStep(_laterStepId);
        StoredJobStep(_stepId).Status = JobStepStatus.Running;
        StoredJobStep(_stepId).Stage = JobStepStage.First;
        StoredJobStep(_stepId).IsPrepared = true;
        StoredJobStep(_laterStepId).Status = JobStepStatus.Unknown;
        StoredJobStep(_laterStepId).Stage = new JobStepStage(10);
        StoredJobStep(_laterStepId).IsPrepared = true;
        _job.CurrentState.Progress.TotalSteps = 2;
    }

    Task Because() => _job.FinalizeStop();

    [Fact] void should_tell_the_running_step_to_stop() => _step.Verify(step => step.OnJobStopped(), Times.Once);
    [Fact] void should_not_tell_the_step_in_the_stage_that_never_started_to_stop() => _laterStep.Verify(step => step.OnJobStopped(), Times.Never);
    [Fact] void should_not_run_the_stopped_hook_while_a_step_is_still_running() => _job.StoppedCount.ShouldEqual(0);
    [Fact] void should_not_mark_the_stop_finalized() => _job.CurrentState.StopFinalized.ShouldBeFalse();
}
