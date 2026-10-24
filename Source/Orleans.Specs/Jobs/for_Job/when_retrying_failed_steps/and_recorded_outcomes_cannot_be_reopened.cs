// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_retrying_failed_steps;

public class and_recorded_outcomes_cannot_be_reopened : given.a_job_that_completed_with_failures
{
    void Establish()
    {
        _job.RetryFailedStepsAllowed = true;
        _job.CurrentState.CompletedStepsCounted = [_succeededId, _failedId];
        _job.FailWrites = true;
    }

    async Task Because() => await _job.RetryFailed();

    [Fact] void should_restore_both_records() => _job.CurrentState.CompletedStepsCounted.ShouldContainOnly(_succeededId, _failedId);
    [Fact] void should_restore_the_failed_count() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(1);
}
