// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_a_legacy_starting_job_has_no_plan : given.a_job_left_running
{
    void Establish() => _job.CurrentState.Status = JobStatus.StartingSteps;

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_fail_with_a_typed_reason() => _result.AsT1.AsT0.ShouldEqual(CannotResumeJobError.MissingStepPlan);
    [Fact] void should_not_replan() => _job.PrepareStepsCount.ShouldEqual(0);
    [Fact] void should_not_dispatch() => _unfinished.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
}
