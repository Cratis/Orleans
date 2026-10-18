// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Cratis.Orleans.Jobs.for_Job.when_removing;

public class and_a_failed_notification_succeeds_on_retry : given.a_job_with_subscribed_steps
{
    async Task Establish()
    {
        _firstObserver.OnJobRemoved().Returns(_ => NotificationFailed);
        await _job.Remove();
        await _job.OnStepStopped(_stepIds[1], Stopped);
        _firstObserver.OnJobRemoved().Returns(async _ => await _job.OnStepStopped(_stepIds[0], Stopped));
    }

    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_retry_the_failed_step() => _firstObserver.Received(2).OnJobRemoved();
    [Fact] void should_not_notify_the_answered_step_again() => _secondObserver.Received(1).OnJobRemoved();
    [Fact] void should_finish_removal_once_the_step_answers() => _silo.StorageStats<given.SomeJob, given.SomeJobState>().Clears.ShouldEqual(1);
    [Fact] void should_run_the_stopped_hook_once() => _job.StoppedCount.ShouldEqual(1);
    [Fact] void should_leave_no_retry_armed() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
