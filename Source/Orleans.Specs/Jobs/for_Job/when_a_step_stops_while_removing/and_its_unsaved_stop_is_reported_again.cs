// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Orleans.TestKit;

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_stops_while_removing;

public class and_its_unsaved_stop_is_reported_again : given.a_job_being_removed_with_two_steps
{
    Result<JobError> _result;

    async Task Establish()
    {
        _job.FailWrites = true;
        await _job.OnStepStopped(_stepId, Stopped);
        _job.FailWrites = false;
    }

    async Task Because() => _result = await _job.OnStepStopped(_stepId, Stopped);

    [Fact] void should_acknowledge_the_report() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_count_the_stop_once() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(1);
    [Fact] void should_save_the_count() => _job.WrittenStates[^1].Progress.StoppedSteps.ShouldEqual(1);
    [Fact] void should_save_which_step_was_counted() => _job.WrittenStates[^1].StoppedStepsCounted.ShouldContainOnly(_stepId);
    [Fact] void should_hand_the_stop_to_the_job_once() => _job.HandledStepResults.ShouldContainOnly(_stepId);
    [Fact] void should_not_finish_the_removal_while_a_step_has_not_stopped() => _silo.StorageStats<given.SomeJob, given.SomeJobState>().Clears.ShouldEqual(0);
}
