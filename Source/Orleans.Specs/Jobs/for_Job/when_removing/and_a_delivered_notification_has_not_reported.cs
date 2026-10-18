// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_removing;

public class and_a_delivered_notification_has_not_reported : given.a_job_with_subscribed_steps
{
    async Task Establish()
    {
        await _job.Remove();
        await _job.OnStepStopped(_stepIds[1], Stopped);
    }

    Task Because() => _job.Remove();

    [Fact] void should_deliver_again_to_the_unreported_step() => _firstObserver.Received(2).OnJobRemoved();
    [Fact] void should_not_deliver_again_to_the_reported_step() => _secondObserver.Received(1).OnJobRemoved();
    [Fact] void should_not_schedule_automatic_retries_for_successful_delivery() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
