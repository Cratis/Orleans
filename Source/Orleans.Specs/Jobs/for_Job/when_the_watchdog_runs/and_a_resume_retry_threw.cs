// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_the_watchdog_runs;

public class and_a_resume_retry_threw : when_retrying_failed_steps.given.a_job_that_completed_with_failures
{
    Result<ResumeJobSuccess, ResumeJobError> _resumeResult;

    async Task Establish()
    {
        _job.RetryFailedStepsAllowed = true;
        await _job.RetryFailed();
        StoredJobStep(_failedId).Status = JobStepStatus.Running;
        _failed.Invocations.Clear();
        _failed.Setup(step => step.Retry(It.IsAny<GrainId>(), 2)).ThrowsAsync(new TimeoutException("Retry timed out"));
        _resumeResult = await _job.Resume();
        _failed.Setup(step => step.Retry(It.IsAny<GrainId>(), 2)).ReturnsAsync(Result<StartJobStepError>.Success());
        CountStepsInStorage((JobStepStatus.Running, 1));
    }

    async Task Because()
    {
        await _job.RunWatchdog();
        await _job.RunWatchdog();
    }

    [Fact] void should_report_the_failed_resume_dispatch() => ((FailedResumingJobSteps)(ResumeJobError)_resumeResult).FailedJobSteps.ShouldContainOnly(_failedId);
    [Fact] void should_keep_the_same_attempt_until_dispatch_succeeds() => _failed.Verify(step => step.Retry(It.IsAny<GrainId>(), 2), Times.Exactly(2));
    [Fact] void should_not_start_a_new_execution_of_the_step() => _failed.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
}
