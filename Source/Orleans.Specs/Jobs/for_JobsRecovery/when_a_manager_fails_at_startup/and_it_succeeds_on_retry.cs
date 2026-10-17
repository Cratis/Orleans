// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsRecovery.when_a_manager_fails_at_startup;

public class and_it_succeeds_on_retry : given.a_recovery_participant
{
    readonly TaskCompletionSource _rehydrated = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish() => _first.Rehydrate().Returns(
        _ => Task.FromException(new Exception("manager unavailable")),
        _ =>
        {
            _rehydrated.TrySetResult();
            return Task.CompletedTask;
        });

    async Task Because()
    {
        await _observer.OnStart(CancellationToken.None);
        var delay = await _timeProvider.NextDelay(Deadline);
        delay.Elapse();
        await _rehydrated.Task.WaitAsync(Deadline, TimeProvider.System);
        await _observer.OnStop(CancellationToken.None).WaitAsync(Deadline, TimeProvider.System);
    }

    [Fact] void should_retry_the_failed_manager_without_a_membership_change() => _first.Received(2).Rehydrate();
    [Fact] void should_not_retry_the_healthy_manager() => _second.Received(1).Rehydrate();
    [Fact] void should_not_enumerate_the_healthy_provider_again() => _keys.Received(1).GetKeys(Arg.Any<CancellationToken>());
}
