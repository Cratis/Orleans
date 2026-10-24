// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_retrying_failed_steps;

public class and_the_reopened_step_succeeds : given.a_job_that_completed_with_failures
{
    async Task Establish()
    {
        _job.RetryFailedStepsAllowed = true;
        _job.CurrentState.CompletedStepsCounted = [_succeededId, _failedId];
        _failed.Setup(step => step.Retry(It.IsAny<GrainId>(), It.IsAny<int>())).ReturnsAsync(Result<StartJobStepError>.Success());
        await _job.RetryFailed();
    }

    async Task Because() => await _job.OnStepSucceeded(_failedId, JobStepResult.Succeeded());

    [Fact] void should_record_the_reopened_steps_new_outcome() => _job.CurrentState.CompletedStepsCounted.ShouldContainOnly(_succeededId, _failedId);
    [Fact] void should_count_both_successes() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(2);
    [Fact] void should_not_count_the_old_failure() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(0);
    [Fact] void should_handle_the_new_outcome() => _job.HandledStepResults.ShouldContainOnly(_failedId);
}
