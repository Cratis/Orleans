// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_persisting_resume_fails_with_a_stop_notification_pending : given.a_job_with_subscribed_steps
{
    async Task Establish()
    {
        _firstObserver.OnJobStopped().Returns(_ => NotificationFailed);
        await _job.Stop();
        _job.ShouldBeResumable = true;
        _job.FailWrites = true;
    }

    async Task Because()
    {
        await _job.Resume();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_leave_the_job_stopped() => _job.CurrentState.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_keep_retrying_the_stop() => _firstObserver.Received(2).OnJobStopped();
    [Fact] void should_keep_one_retry_timer() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(1);
}
