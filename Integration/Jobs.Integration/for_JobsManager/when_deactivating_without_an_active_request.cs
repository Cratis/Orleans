// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.for_JobsManager;

public class when_deactivating_without_an_active_request : given.a_jobs_manager_with_lifecycle
{
    bool _firstReadWasStillPending;

    async Task Establish()
    {
        await _manager.Rehydrate().WaitAsync(Deadline);
        await _lifecycle.FirstReadStarted.Task.WaitAsync(Deadline);
    }

    async Task Because()
    {
        // Unlike DeactivateWhileBusy, this returns immediately: no request holds teardown open for the drain.
        await _manager.Deactivate().WaitAsync(Deadline);
        await _lifecycle.DeactivationCompleted.Task.WaitAsync(Deadline);
        _firstReadWasStillPending = !_lifecycle.AllowFirstRead.Task.IsCompleted;
        await _lifecycle.BackgroundDrain.WaitAsync(Deadline);
    }

    [Fact] void should_finish_the_drain_before_completing_the_deactivation_hook() => _lifecycle.DrainWasCompleteWhenDeactivated.ShouldBeTrue();
    [Fact] void should_not_fault_the_detached_drain() => _lifecycle.BackgroundDrain.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_not_wait_for_the_in_flight_storage_read() => _firstReadWasStillPending.ShouldBeTrue();
    [Fact] void should_not_dispatch_another_job() => _lifecycle.JobStorage.Received(1).GetJob(Arg.Any<JobId>());
}
