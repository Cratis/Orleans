// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_delivering;

public class and_the_job_never_acknowledges_a_failure : given.a_step_that_retries_delivery_without_waiting
{
    protected override int DeliveryAttempts => 3;

    void Establish()
    {
        _jobStep.PerformedResult = JobStepResult.Failed("failed");
        _jobStep.ReportedJob.OnStepFailed(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>()).Returns(Result<JobError>.Failed(JobError.UnknownError));
    }

    Task Because() => RunToDelivery();

    [Fact] async Task should_deliver_as_many_times_as_it_may() => await _jobStep.ReportedJob.Received(3).OnStepFailed(_jobStepId, Arg.Any<JobStepResult>());
    [Fact] void should_leave_the_failure_persisted() => _storage.Persisted[^1].Status.ShouldEqual(JobStepStatus.CompletedWithFailure);
    [Fact] void should_not_write_failed_and_give_up() => _storage.Persisted.Any(_ => _.Status == JobStepStatus.Failed).ShouldBeFalse();
}
