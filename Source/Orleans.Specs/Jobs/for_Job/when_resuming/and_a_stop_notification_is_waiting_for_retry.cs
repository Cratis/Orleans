// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_a_stop_notification_is_waiting_for_retry : given.a_job_with_subscribed_steps
{
    async Task Establish()
    {
        _firstObserver.OnJobStopped().Returns(_ => NotificationFailed);
        await _job.Stop();
        _job.ShouldBeResumable = true;
    }

    async Task Because()
    {
        await _job.Resume();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_resume_the_job() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_not_deliver_the_old_stop_again() => _firstObserver.Received(1).OnJobStopped();
    [Fact] void should_leave_only_the_watchdog_timer() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(1);
}
