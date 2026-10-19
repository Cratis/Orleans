// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Orleans.TestKit;

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_stops_while_removing;

public class and_its_stop_is_reported_again_after_clearing_failed : given.a_job_being_removed_with_two_steps
{
    Result<JobError> _result;

    async Task Establish()
    {
        await _job.OnStepStopped(_secondStepId, Stopped);
        _job.FailClears = true;
        await _job.OnStepStopped(_stepId, Stopped);
        _job.FailClears = false;
    }

    async Task Because() => _result = await _job.OnStepStopped(_stepId, Stopped);

    [Fact] void should_acknowledge_the_report() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_finish_the_removal_by_clearing_the_job() => _silo.StorageStats<given.SomeJob, given.SomeJobState>().Clears.ShouldEqual(1);
    [Fact] void should_hand_each_stop_to_the_job_once() => _job.HandledStepResults.ShouldContainOnly(_secondStepId, _stepId);
}
