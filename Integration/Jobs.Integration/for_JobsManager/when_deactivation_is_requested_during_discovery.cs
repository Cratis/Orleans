// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.for_JobsManager;

public class when_deactivation_is_requested_during_discovery : given.a_jobs_manager_with_lifecycle
{
    Task _rehydration;
    Exception _error;

    async Task Establish()
    {
        _lifecycle.PauseDiscovery = true;
        _rehydration = _manager.Rehydrate();
        await _lifecycle.DiscoveryStarted.Task.WaitAsync(Deadline);
    }

    async Task Because()
    {
        await _manager.Deactivate().WaitAsync(Deadline);

        // Discovery completes only after Orleans has emitted its non-replayed Deactivating event.
        _lifecycle.AllowDiscovery.SetResult();
        _error = await Specifications.Catch.Exception(() => _rehydration.WaitAsync(Deadline));
        await _lifecycle.DeactivationCompleted.Task.WaitAsync(Deadline);
    }

    [Fact] void should_cancel_preparation() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_start_a_background_drain() => _lifecycle.BackgroundDrain.ShouldBeNull();
    [Fact] void should_not_dispatch_any_discovered_jobs() => _lifecycle.JobStorage.DidNotReceive().GetJob(Arg.Any<JobId>());
}
