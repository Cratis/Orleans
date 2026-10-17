// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsRecovery.when_a_key_provider_fails_at_startup;

public class and_its_discovered_manager_fails : given.a_recovery_participant
{
    readonly TaskCompletionSource _rehydrated = new(TaskCreationOptions.RunContinuationsAsynchronously);
    TimeSpan _secondDelay;

    void Establish()
    {
        _keys.GetKeys(Arg.Any<CancellationToken>()).Returns(
            Task.FromException<IEnumerable<JobsManagerKey>>(new Exception("catalog unavailable")),
            Task.FromResult<IEnumerable<JobsManagerKey>>([new("scope", "first")]));
        _first.Rehydrate().Returns(
            _ => Task.FromException(new Exception("manager unavailable")),
            _ =>
            {
                _rehydrated.TrySetResult();
                return Task.CompletedTask;
            });
    }

    async Task Because()
    {
        await _observer.OnStart(CancellationToken.None);
        var firstDelay = await _timeProvider.NextDelay(Deadline);
        firstDelay.Elapse();
        var secondDelay = await _timeProvider.NextDelay(Deadline);
        _secondDelay = secondDelay.Duration;
        secondDelay.Elapse();
        await _rehydrated.Task.WaitAsync(Deadline, TimeProvider.System);
        await _observer.OnStop(CancellationToken.None).WaitAsync(Deadline, TimeProvider.System);
    }

    [Fact] void should_retry_the_newly_discovered_failed_manager() => _first.Received(2).Rehydrate();
    [Fact] void should_not_retry_the_provider_after_its_catalog_succeeds() => _keys.Received(2).GetKeys(Arg.Any<CancellationToken>());
    [Fact] void should_continue_the_backoff_for_the_pending_manager() => _secondDelay.ShouldEqual(TimeSpan.FromSeconds(2));
}
