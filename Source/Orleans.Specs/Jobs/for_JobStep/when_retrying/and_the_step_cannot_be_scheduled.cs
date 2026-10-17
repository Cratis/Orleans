// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_retrying;

public class and_the_step_cannot_be_scheduled : given.a_job_step_left_scheduled_by_a_dead_process
{
    Result<StartJobStepError> _result;

    void Establish()
    {
        _persistedState.Status = JobStepStatus.CompletedWithFailure;
        _persistedState.Attempt = 1;
        _storage.FailWrites = true;
    }

    async Task Because() => _result = await _jobStep.Retry(GrainId.Create("job", _jobId.ToString()), 2);

    [Fact] void should_not_start_the_attempt() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_leave_the_failure_as_it_was() => _persistedState.Status.ShouldEqual(JobStepStatus.CompletedWithFailure);
    [Fact] void should_stay_on_the_attempt_that_failed() => _persistedState.Attempt.ShouldEqual(1);
}
