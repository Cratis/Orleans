// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Orleans.TestKit;

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_stops_while_removing;

public class and_a_later_activation_already_counted_it : given.a_job_being_removed_with_two_steps
{
    Result<JobError> _result;

    void Establish()
    {
        // What an earlier activation saved when it counted the stop, before the step heard back.
        _job.CurrentState.StoppedStepsCounted = [_stepId];
        _job.CurrentState.Progress.StoppedSteps = 1;
    }

    async Task Because() => _result = await _job.OnStepStopped(_stepId, Stopped);

    [Fact] void should_acknowledge_the_report() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_count_the_stop_again() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(1);
    [Fact] void should_not_hand_the_stop_to_the_job_again() => _job.HandledStepResults.ShouldBeEmpty();
    [Fact] void should_not_finish_the_removal_while_a_step_has_not_stopped() => _silo.StorageStats<given.SomeJob, given.SomeJobState>().Clears.ShouldEqual(0);
}
