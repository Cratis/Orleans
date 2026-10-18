// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Stages;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_plan_was_saved_before_any_preparation : given.a_job_with_a_durable_plan
{
    async Task Establish()
    {
        _first.Setup(step => step.Prepare(It.IsAny<object>(), It.IsAny<JobStepStage>()))
            .ThrowsAsync(new InvalidOperationException("The silo stopped"));
        await _job.Start(new());
        await ActivateFrom(JobStatus.PreparingSteps);
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_resume() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_replan() => _job.PrepareStepsCount.ShouldEqual(0);
    [Fact] void should_prepare_the_first_step_with_its_request() => _first.Verify(step => step.Prepare(It.IsAny<given.SomeRequest>(), JobStepStage.First), Times.Once);
    [Fact] void should_keep_the_full_plan_during_preparation() => _job.WrittenStates.First(state => state.Status is JobStatus.PreparingSteps).StepPlan!.Select(step => step.Id).ShouldEqual([_firstId, _secondId, _laterId]);
    [Fact] void should_release_the_plan_after_startup() => _job.CurrentState.StepPlan.ShouldBeNull();
    [Fact] void should_dispatch_only_the_first_stage() => _dispatched.ShouldEqual(new[] { _firstId, _secondId });
    [Fact] void should_signal_incomplete_initialization() => _job.DispatchContexts.Single().InitializationMayBeIncomplete.ShouldBeTrue();
}
