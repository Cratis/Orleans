// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_a_stage_completes_while_the_resume_hook_is_owed;

public class and_the_completed_stage_failed_as_a_barrier : given.a_job_with_a_durable_plan
{
    async Task Establish()
    {
        await _job.Start(new());
        await ActivateFrom(JobStatus.Running);
        (await _job.OnStepSucceeded(_firstId, JobStepResult.Succeeded())).IsSuccess.ShouldBeTrue();
    }

    Task Because() => _job.OnStepFailed(_secondId, JobStepResult.Failed("failed"));

    [Fact] void should_not_start_the_next_stage() => _later.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_record_the_later_stage_unreachable() => _job.CurrentState.Progress.Stages[1].UnreachableSteps.ShouldEqual(1);
    [Fact] void should_complete_with_failures() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedWithFailures);
}
