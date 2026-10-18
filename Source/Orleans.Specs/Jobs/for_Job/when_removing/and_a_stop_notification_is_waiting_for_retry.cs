// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_removing;

public class and_a_stop_notification_is_waiting_for_retry : given.a_job_with_subscribed_steps
{
    List<long?> _generations = [];

    async Task Establish()
    {
        _firstObserver.OnJobStopped().Returns(_ => NotificationFailed);
        _firstObserver.OnJobRemoved().Returns(_ =>
        {
            _generations.Add(JobExecutionGeneration.Take());
            return NotificationFailed;
        });
        await _job.Stop();
    }

    async Task Because()
    {
        await _job.Remove();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_retry_removal_rather_than_the_superseded_stop() => _firstObserver.Received(2).OnJobRemoved();
    [Fact] void should_not_retry_the_stop() => _firstObserver.Received(1).OnJobStopped();
    [Fact] void should_carry_the_execution_generation_on_both_deliveries() => _generations.ShouldEqual([1L, 1L]);
    [Fact] void should_keep_only_one_retry_timer() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(1);
}
