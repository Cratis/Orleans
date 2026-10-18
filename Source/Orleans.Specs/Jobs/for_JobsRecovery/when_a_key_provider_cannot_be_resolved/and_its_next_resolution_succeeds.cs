// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsRecovery.when_a_key_provider_cannot_be_resolved;

public class and_its_next_resolution_succeeds : given.a_recovery_participant_with_unresolved_provider
{
    protected override int FailedResolutions => 1;

    async Task Because()
    {
        await _observer.OnStart(CancellationToken.None);
        await _startup.Task.WaitAsync(Deadline, TimeProvider.System);
        await _observer.OnStop(CancellationToken.None).WaitAsync(Deadline, TimeProvider.System);
    }

    [Fact] void should_resolve_the_catalog_again_without_a_membership_change() => _resolutions.ShouldEqual(2);
    [Fact] void should_rehydrate_the_previously_unavailable_manager() => _first.Received(1).Rehydrate();
    [Fact] void should_recover_the_healthy_manager_once() => _second.Received(1).Rehydrate();
}
