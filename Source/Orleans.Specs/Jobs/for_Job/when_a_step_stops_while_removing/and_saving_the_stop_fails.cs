// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_stops_while_removing;

public class and_saving_the_stop_fails : given.a_job_being_removed_with_two_steps
{
    Result<JobError> _result;

    void Establish() => _job.FailWrites = true;

    async Task Because() => _result = await _job.OnStepStopped(_stepId, Stopped);

    [Fact] void should_answer_that_the_stop_was_not_saved() => (_result.TryGetError(out var error) ? error : JobError.UnknownError).ShouldEqual(JobError.PersistStateError);
    [Fact] void should_count_the_stop() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(1);
}
