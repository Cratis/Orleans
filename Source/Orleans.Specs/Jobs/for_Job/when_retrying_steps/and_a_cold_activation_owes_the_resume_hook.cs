// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_retrying_steps;

public class and_a_cold_activation_owes_the_resume_hook : given.a_job_with_a_durable_plan
{
    protected override JobsOptions CreateOptions() => new() { MaxConcurrentStepStarts = 1, MaxStepAttempts = 2, StepRetryBackoff = TimeSpan.Zero };

    async Task Establish()
    {
        await _job.Start(new());
        StoredJobStep(_firstId).Status = JobStepStatus.Running;
        await ActivateFrom(JobStatus.Running);
        _first.Setup(step => step.Retry(It.IsAny<GrainId>(), It.IsAny<int>())).ReturnsAsync(Result<StartJobStepError>.Success());
        RequestContext.Set(JobStepAttempt.Key, 1);
        (await _job.OnStepFailed(_firstId, JobStepResult.Failed("failed"))).IsSuccess.ShouldBeTrue();
    }

    Task Because() => _job.RetryDueSteps();

    [Fact] void should_not_retry() => _first.Verify(_ => _.Retry(It.IsAny<GrainId>(), It.IsAny<int>()), Times.Never);
    [Fact] void should_not_have_run_the_hook() => _job.BeforeResumingCount.ShouldEqual(0);
    [Fact] void should_stay_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
