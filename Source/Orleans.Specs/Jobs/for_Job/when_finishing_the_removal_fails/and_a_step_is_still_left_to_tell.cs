// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_finishing_the_removal_fails;

public class and_a_step_is_still_left_to_tell : given.a_job_being_removed_whose_current_run_cannot_be_told
{
    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_tell_the_current_run_again() => _currentRun.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IJobObserver.OnJobRemoved)).ShouldBeGreaterThanOrEqual(2);
    [Fact] void should_retry_finishing_the_removal_again() => RemovalRetriesArmed.ShouldEqual(2);
    [Fact] void should_keep_the_current_run_subscribed() => CurrentRunIsSubscribed.ShouldBeTrue();
}
