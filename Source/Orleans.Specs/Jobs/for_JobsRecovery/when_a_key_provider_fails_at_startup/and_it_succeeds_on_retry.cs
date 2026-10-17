// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsRecovery.when_a_key_provider_fails_at_startup;

public class and_it_succeeds_on_retry : given.a_recovery_participant
{
    readonly TaskCompletionSource _rehydrated = new(TaskCreationOptions.RunContinuationsAsynchronously);
    TimeSpan _delay;
    IJobsManagerKeys _healthy;

    protected override IInstancesOf<IJobsManagerKeys> CreateProviders(IJobsManagerKeys keys)
    {
        _healthy = Substitute.For<IJobsManagerKeys>();
        _healthy.GetKeys(Arg.Any<CancellationToken>()).Returns(
            Task.FromResult<IEnumerable<JobsManagerKey>>([new("scope", "second")]));
        return new KnownInstancesOf<IJobsManagerKeys>(keys, _healthy);
    }

    void Establish()
    {
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
        await _observer.OnStart(CancellationToken.None);
        var delay = await _timeProvider.NextDelay(Deadline);
        _delay = delay.Duration;
        delay.Elapse();
        await _rehydrated.Task.WaitAsync(Deadline, TimeProvider.System);
        await _observer.OnStop(CancellationToken.None).WaitAsync(Deadline, TimeProvider.System);
    }

    [Fact] void should_retry_the_catalog_without_a_membership_change() => _keys.Received(2).GetKeys(Arg.Any<CancellationToken>());
    [Fact] void should_rehydrate_the_discovered_manager() => _first.Received(1).Rehydrate();
    [Fact] void should_not_retry_the_healthy_provider() => _healthy.Received(1).GetKeys(Arg.Any<CancellationToken>());
    [Fact] void should_not_retry_the_healthy_providers_manager() => _second.Received(1).Rehydrate();
    [Fact] void should_wait_one_second_before_retrying() => _delay.ShouldEqual(TimeSpan.FromSeconds(1));
}
