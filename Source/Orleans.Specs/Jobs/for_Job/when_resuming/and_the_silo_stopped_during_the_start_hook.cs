// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Stages;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_silo_stopped_during_the_start_hook : given.a_job_with_a_durable_plan
{
    async Task Establish()
    {
        _job.BeforeStarting = () => Task.FromException(new InvalidOperationException("The silo stopped in the hook"));
        await _job.Start(new());
        await ActivateFrom(JobStatus.StartingSteps);
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_resume() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_replan() => _job.PrepareStepsCount.ShouldEqual(0);
    [Fact] void should_not_reprepare() => _first.Verify(step => step.Prepare(It.IsAny<object>(), It.IsAny<JobStepStage>()), Times.Never);
    [Fact] void should_not_replay_the_start_hook() => _job.BeforeStartingCount.ShouldEqual(0);
    [Fact] void should_run_the_new_hook_before_any_step() => _job.DispatchContexts.Single().InitializationMayBeIncomplete.ShouldBeTrue();
    [Fact] void should_reuse_the_ids() => _dispatched.ShouldEqual(new[] { _firstId, _secondId });
}
