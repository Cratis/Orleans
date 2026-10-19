// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_stops_while_removing;

public class and_clearing_the_job_fails : given.a_job_being_removed_with_two_steps
{
    Result<JobError> _result;

    async Task Establish()
    {
        await _job.OnStepStopped(_secondStepId, Stopped);
        _job.FailClears = true;
    }

    async Task Because() => _result = await _job.OnStepStopped(_stepId, Stopped);

    [Fact] void should_answer_with_an_error() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_try_to_clear_the_job() => _job.ClearAttempts.ShouldEqual(1);
    [Fact] void should_count_every_stop() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(2);
}
