// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_delivering;

public class and_delivering_a_success_throws : given.a_step_that_retries_delivery_without_waiting
{
    int _deliveries;

    void Establish() =>
        _jobStep.ReportedJob.OnStepSucceeded(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>()).Returns(_ =>
            ++_deliveries < 3
                ? Task.FromException<Result<JobError>>(new TimeoutException("Response did not arrive on time"))
                : Task.FromResult(Result<JobError>.Success()));

    Task Because() => RunToDelivery();

    [Fact] void should_deliver_until_the_job_acknowledges() => _deliveries.ShouldEqual(3);
    [Fact] void should_never_write_failed() => _storage.Persisted.Any(_ => _.Status == JobStepStatus.Failed).ShouldBeFalse();
}
