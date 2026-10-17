// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_delivering;

public class and_the_job_never_acknowledges_a_success : given.a_step_that_retries_delivery_without_waiting
{
    protected override int DeliveryAttempts => 3;

    void Establish() =>
        _jobStep.ReportedJob.OnStepSucceeded(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>()).Returns(Result<JobError>.Failed(JobError.UnknownError));

    Task Because() => RunToDelivery();

    [Fact] async Task should_deliver_as_many_times_as_it_may() => await _jobStep.ReportedJob.Received(3).OnStepSucceeded(_jobStepId, Arg.Any<JobStepResult>());
    [Fact] void should_not_write_failed_and_give_up() => _storage.Persisted.Any(_ => _.Status == JobStepStatus.Failed).ShouldBeFalse();
    [Fact] void should_leave_the_persisted_success_for_the_watchdog() => _storage.Persisted[^1].Status.ShouldEqual(JobStepStatus.CompletedSuccessfully);
}
