// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsRecovery;

public class when_a_key_provider_fails_after_a_member_dies : given.a_recovery_participant
{
    readonly TaskCompletionSource _rehydrated = new(TaskCreationOptions.RunContinuationsAsynchronously);

    async Task Establish()
    {
        await Start();
        _keys.GetKeys(Arg.Any<CancellationToken>()).Returns(
            Task.FromException<IEnumerable<JobsManagerKey>>(new Exception("catalog unavailable")),
            Task.FromResult<IEnumerable<JobsManagerKey>>([new("scope", "first")]));
        _first.Rehydrate().Returns(_ =>
        {
            _rehydrated.TrySetResult();
            return Task.CompletedTask;
        });
    }

    async Task Because()
    {
        await Publish(SiloStatus.Dead);
        var delay = await _timeProvider.NextDelay(Deadline);
        delay.Elapse();
        await _rehydrated.Task.WaitAsync(Deadline, TimeProvider.System);
        await _observer.OnStop(CancellationToken.None).WaitAsync(Deadline, TimeProvider.System);
    }

    [Fact] void should_retry_the_failed_member_recovery_pass() => _first.Received(1).Rehydrate();
    [Fact] void should_not_require_another_death_to_retry_the_catalog() => _keys.Received(3).GetKeys(Arg.Any<CancellationToken>());
}
