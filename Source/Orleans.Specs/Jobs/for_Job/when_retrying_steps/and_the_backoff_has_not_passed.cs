// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_retrying_steps;

public class and_the_backoff_has_not_passed : given.a_running_job_with_steps
{
    protected override JobsOptions CreateOptions() => new() { MaxStepAttempts = 3, StepRetryBackoff = TimeSpan.FromHours(1) };

    async Task Because()
    {
        await Fail(0);
        await _job.RetryDueSteps();
    }

    [Fact] void should_wait() => _steps[0].Verify(_ => _.Retry(It.IsAny<GrainId>(), It.IsAny<int>()), Times.Never);
}
