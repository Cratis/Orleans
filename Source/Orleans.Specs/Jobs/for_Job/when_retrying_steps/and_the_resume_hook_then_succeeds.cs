// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_retrying_steps;

public class and_the_resume_hook_then_succeeds : given.a_running_job_with_steps
{
    protected override JobsOptions CreateOptions() => new() { MaxStepAttempts = 3, StepRetryBackoff = TimeSpan.Zero };

    async Task Establish()
    {
        await Fail(0);
        _job.BeforeResuming = () => Task.FromException(new InvalidOperationException("Resume was rejected"));
        (await _job.Resume()).IsSuccess.ShouldBeFalse();
        await _job.RetryDueSteps();
        _job.BeforeResuming = () => Task.CompletedTask;
        CountStepsInStorage((JobStepStatus.Running, 2));
    }

    async Task Because()
    {
        await _job.RunWatchdog();
        await _job.RetryDueSteps();
    }

    [Fact] void should_run_the_second_attempt_once() => _steps[0].Verify(_ => _.Retry(It.IsAny<GrainId>(), 2), Times.Once);
    [Fact] void should_have_retried_the_hook() => _job.BeforeResumingCount.ShouldEqual(2);
    [Fact] void should_not_count_a_failure() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(0);
}
