// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_retrying;

public class and_the_attempt_was_already_run : given.a_job_step_left_scheduled_by_a_dead_process
{
    Result<StartJobStepError> _result;

    void Establish()
    {
        _persistedState.Status = JobStepStatus.CompletedWithFailure;
        _persistedState.Attempt = 2;
    }

    async Task Because() => _result = await _jobStep.Retry(GrainId.Create("job", _jobId.ToString()), 2);

    [Fact] void should_not_run_it_again() => _result.AsT1.ShouldEqual(StartJobStepError.Completed);
    [Fact] void should_stay_on_the_attempt() => _persistedState.Attempt.ShouldEqual(2);
    [Fact] void should_write_nothing() => _storage.Persisted.Count.ShouldEqual(0);
}
