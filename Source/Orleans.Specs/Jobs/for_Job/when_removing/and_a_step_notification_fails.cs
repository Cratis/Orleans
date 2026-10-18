// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_removing;

public class and_a_step_notification_fails : given.a_job_with_subscribed_steps
{
    void Establish() => _firstObserver.OnJobRemoved().Returns(_ => NotificationFailed);

    Task Because() => _job.Remove();

    [Fact] void should_keep_the_failed_step_subscribed() => _observers.Observers.Keys.ShouldContain(_stepIds[0]);
    [Fact] void should_still_notify_the_other_step() => _secondObserver.Received(1).OnJobRemoved();
    [Fact] void should_wait_for_the_steps_to_report() => _job.CurrentState.Status.ShouldEqual(JobStatus.Removing);
    [Fact] void should_schedule_a_one_second_retry() => NotificationTimerOptions.Last().DueTime.ShouldEqual(TimeSpan.FromSeconds(1));
    [Fact] void should_schedule_only_one_retry() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(1);
}
