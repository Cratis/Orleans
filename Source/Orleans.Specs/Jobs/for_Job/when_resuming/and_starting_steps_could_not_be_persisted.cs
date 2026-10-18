// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Stages;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_starting_steps_could_not_be_persisted : given.a_job_with_a_durable_plan
{
    async Task Establish()
    {
        _job.RejectWrite = state => state.Status is JobStatus.StartingSteps;
        await _job.Start(new());
        await ActivateFrom(JobStatus.PreparingSteps);
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_resume() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_prepare_any_step_twice() => _first.Verify(step => step.Prepare(It.IsAny<object>(), It.IsAny<JobStepStage>()), Times.Never);
    [Fact] void should_keep_later_preparation() => _later.Verify(step => step.Prepare(It.IsAny<object>(), It.IsAny<JobStepStage>()), Times.Never);
    [Fact] void should_not_replay_the_start_hook() => _job.BeforeStartingCount.ShouldEqual(0);
    [Fact] void should_signal_incomplete_initialization() => _job.DispatchContexts.Single().InitializationMayBeIncomplete.ShouldBeTrue();
}
