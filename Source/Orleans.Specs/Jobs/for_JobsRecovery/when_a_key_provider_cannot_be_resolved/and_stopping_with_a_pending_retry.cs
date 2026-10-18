// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsRecovery.when_a_key_provider_cannot_be_resolved;

public class and_stopping_with_a_pending_retry : given.a_recovery_participant_with_unresolved_provider
{
    async Task Because()
    {
        await _observer.OnStart(CancellationToken.None);
        var delay = await _timeProvider.NextDelay(Deadline);
        await _observer.OnStop(CancellationToken.None).WaitAsync(Deadline, TimeProvider.System);
        delay.Elapse();
    }

    [Fact] void should_not_resolve_the_provider_again_after_stopping() => _resolutions.ShouldEqual(2);
    [Fact] void should_not_rehydrate_its_manager() => _first.DidNotReceive().Rehydrate();
}
