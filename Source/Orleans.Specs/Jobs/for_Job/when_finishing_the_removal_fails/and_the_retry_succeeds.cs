// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_finishing_the_removal_fails;

public class and_the_retry_succeeds : given.a_job_whose_removal_could_not_be_finished
{
    void Establish() => _job.FailClears = false;

    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_clear_the_job() => Clears.ShouldEqual(1);
    [Fact] void should_not_schedule_another_retry() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
    [Fact] void should_not_hand_any_stop_to_the_job_again() => _job.HandledStepResults.ShouldContainOnly(_secondStepId, _stepId);
}
