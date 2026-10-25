// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_a_stage_completes_while_the_resume_hook_is_owed;

public class and_the_job_was_activated_cold : given.a_job_with_a_durable_plan
{
    Result<JobError> _outcome;

    async Task Establish()
    {
        await _job.Start(new());
        await ActivateFrom(JobStatus.Running);
        (await _job.OnStepSucceeded(_firstId, JobStepResult.Succeeded())).IsSuccess.ShouldBeTrue();
    }

    async Task Because() => _outcome = await _job.OnStepSucceeded(_secondId, JobStepResult.Succeeded());

    [Fact] void should_accept_the_outcome() => _outcome.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_start_the_next_stage() => _later.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_leave_the_next_stage_unstarted() => _job.CurrentState.Progress.Stages[1].IsStarted.ShouldBeFalse();
    [Fact] void should_not_run_the_dispatch_hook() => _job.DispatchContexts.Count.ShouldEqual(0);
    [Fact] void should_keep_the_job_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_not_complete_the_job() => _job.AllStepsCompletedCount.ShouldEqual(0);
}
