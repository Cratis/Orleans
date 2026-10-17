// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Job.when_retrying_failed_steps;

public class and_the_job_is_not_completed_with_failures : given.a_job_that_completed_with_failures
{
    Result<int, RetryFailedStepsError> _result;

    void Establish()
    {
        _job.RetryFailedStepsAllowed = true;
        _job.CurrentState.Status = JobStatus.Running;
    }

    async Task Because() => _result = await _job.RetryFailed();

    [Fact] void should_refuse() => (_result.TryGetError(out var error) ? error : RetryFailedStepsError.Unknown).ShouldEqual(RetryFailedStepsError.JobIsNotCompletedWithFailures);
}
