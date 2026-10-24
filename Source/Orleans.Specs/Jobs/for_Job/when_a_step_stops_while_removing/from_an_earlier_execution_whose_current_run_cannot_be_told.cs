// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_stops_while_removing;

public class from_an_earlier_execution_whose_current_run_cannot_be_told : given.a_job_being_removed_whose_current_run_cannot_be_told
{
    void Because()
    {
    }

    [Fact] void should_keep_the_current_run_subscribed() => CurrentRunIsSubscribed.ShouldBeTrue();
    [Fact] void should_have_tried_to_tell_it_once() => _currentRun.Received(1).OnJobRemoved();
    [Fact] void should_retry_finishing_the_removal() => RemovalRetriesArmed.ShouldEqual(1);
    [Fact] void should_retry_telling_the_current_run() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(2);
    [Fact] void should_not_have_cleared_the_job() => Clears.ShouldEqual(0);
}
