// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_retrying_failed_steps;

public class and_the_job_type_allows_it : given.a_job_that_completed_with_failures
{
    Result<int, RetryFailedStepsError> _result;

    void Establish() => _job.RetryFailedStepsAllowed = true;

    async Task Because() => _result = await _job.RetryFailed();

    [Fact] void should_run_one_step_again() => _result.Match(count => count, error => -(int)error - 100).ShouldEqual(1);
    [Fact] void should_run_the_failed_step_as_its_second_attempt() => _failed.Verify(_ => _.Retry(It.IsAny<GrainId>(), 2), Times.Once);
    [Fact] void should_run_the_job_again() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_no_longer_count_the_failure() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(0);
    [Fact] void should_keep_the_success() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(1);
}
