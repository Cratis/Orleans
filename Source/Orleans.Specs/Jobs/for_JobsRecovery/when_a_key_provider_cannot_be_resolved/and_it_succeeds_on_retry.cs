// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsRecovery.when_a_key_provider_cannot_be_resolved;

public class and_it_succeeds_on_retry : given.a_recovery_participant_with_unresolved_provider
{
    readonly TaskCompletionSource _rehydrated = new(TaskCreationOptions.RunContinuationsAsynchronously);
    TimeSpan _delay;

    void Establish() => _first.Rehydrate().Returns(_ =>
    {
        _rehydrated.TrySetResult();
        return Task.CompletedTask;
    });

    async Task Because()
    {
        await _observer.OnStart(CancellationToken.None);
        var delay = await _timeProvider.NextDelay(Deadline);
        _delay = delay.Duration;
        delay.Elapse();
        await _rehydrated.Task.WaitAsync(Deadline, TimeProvider.System);
        await _observer.OnStop(CancellationToken.None).WaitAsync(Deadline, TimeProvider.System);
    }

    [Fact] void should_retry_construction_without_a_membership_change() => _resolutions.ShouldEqual(3);
    [Fact] void should_rehydrate_the_discovered_manager() => _first.Received(1).Rehydrate();
    [Fact] void should_not_retry_the_healthy_provider() => _keys.Received(1).GetKeys(Arg.Any<CancellationToken>());
    [Fact] void should_not_retry_the_healthy_providers_manager() => _second.Received(1).Rehydrate();
    [Fact] void should_use_the_existing_retry_delay() => _delay.ShouldEqual(TimeSpan.FromSeconds(1));
}
