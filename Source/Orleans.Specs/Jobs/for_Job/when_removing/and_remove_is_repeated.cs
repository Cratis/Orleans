// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Job.when_removing;

public class and_remove_is_repeated : given.a_job_with_subscribed_steps
{
    Result<RemoveJobError> _result;

    async Task Establish()
    {
        _firstObserver.OnJobRemoved().Returns(_ => NotificationFailed);
        await _job.Remove();
        await _job.OnStepStopped(_stepIds[1], Stopped);
        _firstObserver.OnJobRemoved().Returns(Task.CompletedTask);
    }

    async Task Because() => _result = await _job.Remove();

    [Fact] void should_notify_the_unreported_step_again() => _firstObserver.Received(2).OnJobRemoved();
    [Fact] void should_not_notify_the_answered_step_again() => _secondObserver.Received(1).OnJobRemoved();
    [Fact] void should_keep_the_existing_result() => (_result.TryGetError(out var error) ? error : RemoveJobError.Unknown).ShouldEqual(RemoveJobError.JobIsAlreadyBeingRemoved);
    [Fact] void should_cancel_the_retry_after_delivery_succeeds() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
