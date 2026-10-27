// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_retrying_steps;

public class and_the_job_is_stopped_while_the_resume_hook_is_owed : given.a_running_job_with_steps
{
    protected override JobsOptions CreateOptions() => new() { MaxStepAttempts = 3, StepRetryBackoff = TimeSpan.Zero };

    async Task Establish()
    {
        await Fail(0);
        _job.BeforeResuming = () => Task.FromException(new InvalidOperationException("Resume was rejected"));
        (await _job.Resume()).IsSuccess.ShouldBeFalse();
    }

    async Task Because()
    {
        await _job.RetryDueSteps();
        await _job.Stop();
    }

    [Fact] void should_not_retry() => _steps[0].Verify(_ => _.Retry(It.IsAny<GrainId>(), It.IsAny<int>()), Times.Never);
    [Fact] void should_count_the_held_attempt_as_failed() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(1);
}
