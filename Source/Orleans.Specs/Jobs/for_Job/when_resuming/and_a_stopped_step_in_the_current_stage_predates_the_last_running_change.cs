// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs.Stages;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_a_stopped_step_in_the_current_stage_predates_the_last_running_change : when_running_in_stages.given.a_job_with_three_stages
{
    Result<ResumeJobSuccess, ResumeJobError> _result;

    void Establish()
    {
        _job.CurrentState.Status = JobStatus.Running;
        _job.CurrentState.StatusChanges =
        [
            new() { Status = JobStatus.StartingSteps },
            new() { Status = JobStatus.Running },
            new() { Status = JobStatus.Stopped },
            new() { Status = JobStatus.Running }
        ];
        _job.CurrentState.Progress.TotalSteps = 4;
        _job.CurrentState.Progress.SuccessfulSteps = 1;
        _job.CurrentState.Progress.Stages = JobStages.Plan([0, 10, 10, 20]);
        _job.CurrentState.Progress.Stages[0].IsStarted = true;
        _job.CurrentState.Progress.Stages[0].SuccessfulSteps = 1;
        _job.CurrentState.Progress.Stages[1].IsStarted = true;
        StoredJobStep(_first).Status = JobStepStatus.CompletedSuccessfully;
        StoredJobStep(_secondA).Status = JobStepStatus.Stopped;
        StoredJobStep(_secondA).Stage = 10;
        StoredJobStep(_secondB).Status = JobStepStatus.Stopped;
        StoredJobStep(_secondB).Stage = 10;
        StoredJobStep(_third).Status = JobStepStatus.Scheduled;
        StoredJobStep(_third).Stage = 20;
        _secondStepA.Setup(step => step.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Success());
        _secondStepB.Setup(step => step.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Success());
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_resume_the_current_stage() => ((ResumeJobSuccess)_result).ShouldEqual(ResumeJobSuccess.Success);
    [Fact] void should_restart_the_first_stopped_step() => ShouldHaveStarted(_secondStepA);
    [Fact] void should_restart_the_second_stopped_step() => ShouldHaveStarted(_secondStepB);
    [Fact] void should_not_restart_the_completed_stage() => ShouldNotHaveStarted(_firstStep);
    [Fact] void should_not_start_the_later_stage() => ShouldNotHaveStarted(_thirdStep);
}
