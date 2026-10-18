// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_a_reconstructed_stage_success_arrives_late : given.a_job_with_a_durable_plan
{
    async Task Establish()
    {
        await _job.Start(new());
        _storedJobStepStates.Remove(StoredJobStep(_firstId));
        await ActivateFrom(JobStatus.StartingSteps);
        (await _job.Resume()).IsSuccess.ShouldBeTrue();
    }

    async Task Because() => (await _job.OnStepSucceeded(_firstId, JobStepResult.Succeeded())).IsSuccess.ShouldBeTrue();

    [Fact] void should_count_the_success_only_once() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_preserve_stage_progress() => _job.CurrentState.Progress.Stages[0].SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_not_cross_the_stage_barrier() => _later.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_not_complete_the_job() => _job.AllStepsCompletedCount.ShouldEqual(0);
}
