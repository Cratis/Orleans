// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_an_interrupted_start_has_only_one_stage : given.a_job_with_a_durable_plan
{
    async Task Establish()
    {
        _job.StepsToPrepare.RemoveAll(step => step.Id == _laterId);
        _job.BeforeStarting = () => Task.FromException(new InvalidOperationException("The silo stopped in the hook"));
        await _job.Start(new());
        await ActivateFrom(JobStatus.StartingSteps);
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_resume() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_replan() => _job.PrepareStepsCount.ShouldEqual(0);
    [Fact] void should_not_replay_the_start_hook() => _job.BeforeStartingCount.ShouldEqual(0);
    [Fact] void should_signal_incomplete_initialization() => _job.DispatchContexts.Single().InitializationMayBeIncomplete.ShouldBeTrue();
    [Fact] void should_reuse_the_step_ids() => _dispatched.ShouldEqual(new[] { _firstId, _secondId });
    [Fact] void should_not_dispatch_the_removed_stage() => _later.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
}
