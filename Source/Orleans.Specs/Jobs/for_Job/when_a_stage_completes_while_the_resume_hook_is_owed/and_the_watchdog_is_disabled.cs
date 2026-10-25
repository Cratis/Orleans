// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_a_stage_completes_while_the_resume_hook_is_owed;

public class and_the_watchdog_is_disabled : given.a_job_with_a_durable_plan
{
    protected override JobsOptions CreateOptions() => new() { MaxConcurrentStepStarts = 1, JobWatchdogInterval = TimeSpan.Zero };

    async Task Establish()
    {
        await _job.Start(new());
        await ActivateFrom(JobStatus.Running);
        (await _job.OnStepSucceeded(_firstId, JobStepResult.Succeeded())).IsSuccess.ShouldBeTrue();
    }

    async Task Because() => (await _job.OnStepSucceeded(_secondId, JobStepResult.Succeeded())).IsSuccess.ShouldBeTrue();

    [Fact] void should_start_the_next_stage_once() => _later.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
    [Fact] void should_not_run_the_resume_hook() => _job.BeforeResumingCount.ShouldEqual(0);
}
