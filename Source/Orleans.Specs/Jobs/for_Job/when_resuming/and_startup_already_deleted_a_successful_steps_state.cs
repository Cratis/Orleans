// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Stages;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_startup_already_deleted_a_successful_steps_state : given.a_job_with_a_durable_plan
{
    async Task Establish()
    {
        await _job.Start(new());
        _storedJobStepStates.Remove(StoredJobStep(_firstId));
        await ActivateFrom(JobStatus.StartingSteps);
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_resume() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_recreate_a_successful_step() => _first.Verify(step => step.Prepare(It.IsAny<object>(), It.IsAny<JobStepStage>()), Times.Never);
    [Fact] void should_not_restart_a_successful_step() => _first.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_count_its_success() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_preserve_the_total() => _job.CurrentState.Progress.TotalSteps.ShouldEqual(3);
    [Fact] void should_preserve_stage_progress() => _job.CurrentState.Progress.Stages[0].SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_resume_only_the_remaining_first_stage_step() => _dispatched.ShouldEqual(new[] { _secondId });
}
