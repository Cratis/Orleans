// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_retrying_steps;

public class and_the_backoff_has_passed : given.a_running_job_with_steps
{
    protected override JobsOptions CreateOptions() => new() { MaxStepAttempts = 3, StepRetryBackoff = TimeSpan.Zero };

    async Task Because()
    {
        await Fail(0);
        await _job.RetryDueSteps();
    }

    [Fact] void should_run_the_step_again_as_the_second_attempt() => _steps[0].Verify(_ => _.Retry(It.IsAny<GrainId>(), 2), Times.Once);
    [Fact] void should_not_run_the_other_step_again() => _steps[1].Verify(_ => _.Retry(It.IsAny<GrainId>(), It.IsAny<int>()), Times.Never);
    [Fact] void should_not_count_a_failure() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(0);
}
