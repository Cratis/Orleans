// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Stages;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_hook_throws_before_an_unstarted_stage : given.a_stopped_job_rejecting_resume
{
    void Establish()
    {
        _job.CurrentState.Progress.StoppedSteps = 0;
        _job.CurrentState.Progress.TotalSteps = 2;
        _job.CurrentState.Progress.Stages =
        [
            new JobStageProgress { Stage = 0, TotalSteps = 1 },
            new JobStageProgress { Stage = 10, TotalSteps = 1 }
        ];
        var id = JobStepId.New();
        AddJobStep(id, 10);
        StoredJobStep(id).Status = JobStepStatus.Scheduled;
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_report_failure() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_compensate_using_eligibility_before_stage_preparation() => _job.CurrentState.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_restore_the_first_stage_to_not_started() => _job.CurrentState.Progress.Stages[0].IsStarted.ShouldBeFalse();
    [Fact] void should_leave_the_later_stage_not_started() => _job.CurrentState.Progress.Stages[1].IsStarted.ShouldBeFalse();
    [Fact] void should_restore_fully_stopped_progress() => _job.CurrentState.Progress.IsStopped.ShouldBeTrue();
    [Fact] void should_persist_the_restored_stage_flags() => _job.WrittenStates[^1].Progress.Stages[0].IsStarted.ShouldBeFalse();
    [Fact] void should_not_start_the_first_stage() => _step.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
}
