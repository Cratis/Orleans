// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_delivering;

public class and_the_job_does_not_acknowledge_a_success_at_first : given.a_step_that_retries_delivery_without_waiting
{
    int _deliveries;

    void Establish() =>
        _jobStep.ReportedJob.OnStepSucceeded(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>()).Returns(_ =>
            Task.FromResult(++_deliveries < 3 ? Result<JobError>.Failed(JobError.UnknownError) : Result<JobError>.Success()));

    Task Because() => RunToDelivery();

    [Fact] void should_deliver_until_the_job_acknowledges() => _deliveries.ShouldEqual(3);
    [Fact] void should_have_persisted_the_success_once() => _storage.Persisted.Count(_ => _.Status == JobStepStatus.CompletedSuccessfully).ShouldEqual(1);
    [Fact] void should_never_write_failed() => _storage.Persisted.Any(_ => _.Status == JobStepStatus.Failed).ShouldBeFalse();
    [Fact] void should_leave_the_step_completed_successfully() => _storage.Persisted[^1].Status.ShouldEqual(JobStepStatus.CompletedSuccessfully);
}
