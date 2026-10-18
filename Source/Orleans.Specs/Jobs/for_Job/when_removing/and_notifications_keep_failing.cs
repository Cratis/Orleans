// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_removing;

public class and_notifications_keep_failing : given.a_job_with_subscribed_steps
{
    void Establish() => _firstObserver.OnJobRemoved().Returns(_ => NotificationFailed);

    async Task Because()
    {
        await _job.Remove();
        for (var retry = 0; retry < 8; retry++)
        {
            await _silo.TimerRegistry.FireAllAsync();
        }
    }

    [Fact] void should_double_the_delay_up_to_one_minute() => NotificationTimerOptions.Select(options => options.DueTime.TotalSeconds).ShouldEqual([1, 2, 4, 8, 16, 32, 60, 60, 60]);
    [Fact] void should_not_retry_a_successfully_delivered_notification() => _secondObserver.Received(1).OnJobRemoved();
    [Fact] void should_keep_only_one_retry_armed() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(1);
}
