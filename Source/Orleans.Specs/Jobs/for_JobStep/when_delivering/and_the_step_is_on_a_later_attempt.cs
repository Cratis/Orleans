// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_delivering;

public class and_the_step_is_on_a_later_attempt : given.a_step_that_retries_delivery_without_waiting
{
    object _reportedAttempt;

    void Establish()
    {
        _persistedState.Attempt = 3;
        _jobStep.ReportedJob.OnStepSucceeded(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>()).Returns(_ =>
        {
            _reportedAttempt = RequestContext.Get(JobStepAttempt.Key);
            return Result<JobError>.Success();
        });
    }

    Task Because() => RunToDelivery();

    [Fact] void should_report_the_attempt_it_is_on() => _reportedAttempt.ShouldEqual(3);
}
