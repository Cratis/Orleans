// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_retrying_failed_steps;

public class and_the_reopening_cannot_be_persisted : given.a_job_that_completed_with_failures
{
    Result<int, RetryFailedStepsError> _result;

    void Establish()
    {
        _job.RetryFailedStepsAllowed = true;
        _job.FailWrites = true;
    }

    async Task Because() => _result = await _job.RetryFailed();

    [Fact] void should_say_it_could_not_persist() => (_result.TryGetError(out var error) ? error : RetryFailedStepsError.Unknown).ShouldEqual(RetryFailedStepsError.CouldNotPersist);
    [Fact] void should_leave_the_job_completed_with_failures() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedWithFailures);
    [Fact] void should_keep_counting_the_failure() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(1);
    [Fact] void should_not_run_the_step_again() => _failed.Verify(_ => _.Retry(It.IsAny<GrainId>(), It.IsAny<int>()), Times.Never);
}
