// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_stopping;

public class and_a_failed_notification_succeeds_on_retry : given.a_job_with_subscribed_steps
{
    async Task Establish()
    {
        _firstObserver.OnJobStopped().Returns(_ => NotificationFailed);
        await _job.Stop();
        await _job.OnStepStopped(_stepIds[1], Stopped);
        _firstObserver.OnJobStopped().Returns(async _ => await _job.OnStepStopped(_stepIds[0], Stopped));
    }

    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_retry_the_failed_stop() => _firstObserver.Received(2).OnJobStopped();
    [Fact] void should_not_retry_the_other_step() => _secondObserver.Received(1).OnJobStopped();
    [Fact] void should_settle_the_stop() => _job.CurrentState.Progress.IsStopped.ShouldBeTrue();
    [Fact] void should_run_the_stopped_hook_once() => _job.StoppedCount.ShouldEqual(1);
    [Fact] void should_leave_no_retry_armed() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
