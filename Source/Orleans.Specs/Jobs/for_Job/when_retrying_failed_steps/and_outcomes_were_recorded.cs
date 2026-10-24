// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_retrying_failed_steps;

public class and_outcomes_were_recorded : given.a_job_that_completed_with_failures
{
    void Establish()
    {
        _job.RetryFailedStepsAllowed = true;
        _job.CurrentState.CompletedStepsCounted = [_succeededId, _failedId];
    }

    async Task Because() => await _job.RetryFailed();

    [Fact] void should_keep_the_successful_outcome() => _job.CurrentState.CompletedStepsCounted.ShouldContainOnly(_succeededId);
    [Fact] void should_save_the_uncounted_failure_with_the_reopening() => _job.WrittenStates[^1].CompletedStepsCounted.ShouldContainOnly(_succeededId);
    [Fact] void should_unaccount_the_failure() => _job.WrittenStates[^1].Progress.FailedSteps.ShouldEqual(0);
}
