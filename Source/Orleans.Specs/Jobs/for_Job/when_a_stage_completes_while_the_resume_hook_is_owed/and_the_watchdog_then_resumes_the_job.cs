// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_a_stage_completes_while_the_resume_hook_is_owed;

public class and_the_watchdog_then_resumes_the_job : given.a_job_with_a_durable_plan
{
    async Task Establish()
    {
        await _job.Start(new());
        await ActivateFrom(JobStatus.Running);
        (await _job.OnStepSucceeded(_firstId, JobStepResult.Succeeded())).IsSuccess.ShouldBeTrue();
        (await _job.OnStepSucceeded(_secondId, JobStepResult.Succeeded())).IsSuccess.ShouldBeTrue();
        CountStepsInStorage((JobStepStatus.Unknown, 1));
    }

    Task Because() => _job.RunWatchdog();

    [Fact] void should_run_the_resume_hook_once() => _job.BeforeResumingCount.ShouldEqual(1);
    [Fact] void should_start_the_next_stage_once() => _later.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
    [Fact] void should_mark_the_stage_started() => _job.CurrentState.Progress.Stages[1].IsStarted.ShouldBeTrue();
    [Fact] void should_run_the_dispatch_hook_once() => _job.DispatchContexts.Single().InitializationMayBeIncomplete.ShouldBeFalse();
    [Fact] void should_keep_the_job_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
