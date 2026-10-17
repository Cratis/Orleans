// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_retrying_failed_steps;

public class and_the_job_is_resumed_instead : given.a_job_that_completed_with_failures
{
    void Establish() => _job.RetryFailedStepsAllowed = true;

    Task Because() => _job.Resume();

    [Fact] void should_not_run_the_failed_step_again() => _failed.Verify(_ => _.Retry(It.IsAny<GrainId>(), It.IsAny<int>()), Times.Never);
    [Fact] void should_leave_the_job_completed_with_failures() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedWithFailures);
}
