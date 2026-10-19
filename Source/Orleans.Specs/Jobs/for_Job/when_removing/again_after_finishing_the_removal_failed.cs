// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Orleans.TestKit;

namespace Cratis.Orleans.Jobs.for_Job.when_removing;

public class again_after_finishing_the_removal_failed : given.a_job_being_removed_with_two_steps
{
    Result<RemoveJobError> _result;

    async Task Establish()
    {
        await _job.OnStepStopped(_secondStepId, Stopped);
        _job.FailClears = true;
        await _job.OnStepStopped(_stepId, Stopped);
        _job.FailClears = false;
    }

    async Task Because() => _result = await _job.Remove();

    [Fact] void should_finish_the_removal_by_clearing_the_job() => _silo.StorageStats<given.SomeJob, given.SomeJobState>().Clears.ShouldEqual(1);
    [Fact] void should_answer_that_the_job_was_already_being_removed() => (_result.TryGetError(out var error) ? error : RemoveJobError.Unknown).ShouldEqual(RemoveJobError.JobIsAlreadyBeingRemoved);
    [Fact] void should_not_hand_any_stop_to_the_job_again() => _job.HandledStepResults.ShouldContainOnly(_secondStepId, _stepId);
}
