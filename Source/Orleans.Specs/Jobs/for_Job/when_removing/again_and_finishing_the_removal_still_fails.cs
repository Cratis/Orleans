// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Job.when_removing;

public class again_and_finishing_the_removal_still_fails : given.a_job_being_removed_with_two_steps
{
    Result<RemoveJobError> _result;

    async Task Establish()
    {
        await _job.OnStepStopped(_secondStepId, Stopped);
        _job.FailClears = true;
        await _job.OnStepStopped(_stepId, Stopped);
    }

    async Task Because() => _result = await _job.Remove();

    [Fact] void should_try_to_clear_the_job_again() => _job.ClearAttempts.ShouldEqual(2);
    [Fact] void should_answer_with_an_error() => (_result.TryGetError(out var error) ? error : RemoveJobError.JobIsAlreadyBeingRemoved).ShouldEqual(RemoveJobError.Unknown);
    [Fact] void should_still_be_removing() => _job.CurrentState.Status.ShouldEqual(JobStatus.Removing);
}
