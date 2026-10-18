// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Stages;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_preparation_stopped_between_steps : given.a_job_with_a_durable_plan
{
    async Task Establish()
    {
        InterruptPreparation();
        await _job.Start(new());
        await ActivateFrom(JobStatus.PreparingSteps);
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_resume() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_replan() => _job.PrepareStepsCount.ShouldEqual(0);
    [Fact] void should_reuse_the_ids() => _dispatched.ShouldEqual(new[] { _firstId, _secondId });
    [Fact] void should_not_prepare_the_prepared_step() => _first.Verify(step => step.Prepare(It.IsAny<object>(), It.IsAny<JobStepStage>()), Times.Never);
    [Fact] void should_prepare_the_unprepared_step() => _second.Verify(step => step.Prepare(It.IsAny<object>(), JobStepStage.First), Times.Once);
    [Fact] void should_prepare_the_later_stage() => _later.Verify(step => step.Prepare(It.IsAny<object>(), (JobStepStage)10), Times.Once);
    [Fact] void should_not_start_the_later_stage() => _later.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_signal_incomplete_initialization() => _job.DispatchContexts.Single().InitializationMayBeIncomplete.ShouldBeTrue();
    [Fact] void should_not_replay_the_start_hook() => _job.BeforeStartingCount.ShouldEqual(0);
    [Fact] void should_call_the_resume_hook() => _job.BeforeResumingCount.ShouldEqual(1);
    [Fact] void should_finish_startup() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
