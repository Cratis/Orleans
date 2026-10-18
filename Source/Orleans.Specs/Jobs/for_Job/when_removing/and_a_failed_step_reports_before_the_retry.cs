// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_removing;

public class and_a_failed_step_reports_before_the_retry : given.a_job_with_subscribed_steps
{
    async Task Establish()
    {
        _firstObserver.OnJobRemoved().Returns(_ => NotificationFailed);
        await _job.Remove();
    }

    async Task Because()
    {
        await _job.OnStepStopped(_stepIds[0], Stopped);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_not_retry_the_step_that_reported() => _firstObserver.Received(1).OnJobRemoved();
    [Fact] void should_cancel_its_retry_timer() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
    [Fact] void should_keep_waiting_for_the_other_step() => _job.CurrentState.Status.ShouldEqual(JobStatus.Removing);
}
