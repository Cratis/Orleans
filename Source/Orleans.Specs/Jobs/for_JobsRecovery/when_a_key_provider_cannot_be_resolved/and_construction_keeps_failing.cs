// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsRecovery.when_a_key_provider_cannot_be_resolved;

public class and_construction_keeps_failing : given.a_recovery_participant_with_unresolved_provider
{
    readonly List<TimeSpan> _delays = [];

    protected override int FailedResolutions => int.MaxValue;

    async Task Because()
    {
        await _observer.OnStart(CancellationToken.None);
        for (var attempt = 0; attempt < 9; attempt++)
        {
            var delay = await _timeProvider.NextDelay(Deadline);
            _delays.Add(delay.Duration);
            if (attempt < 8)
            {
                delay.Elapse();
            }
        }
        await _observer.OnStop(CancellationToken.None).WaitAsync(Deadline, TimeProvider.System);
    }

    [Fact] void should_double_the_delay_and_cap_it_at_sixty_seconds() => _delays.ShouldEqual(new[] { 1, 2, 4, 8, 16, 32, 60, 60, 60 }.Select(seconds => TimeSpan.FromSeconds(seconds)));
    [Fact] void should_keep_retrying_construction_beyond_the_backoff_cap() => _resolutions.ShouldEqual(10);
    [Fact] void should_not_retry_the_healthy_manager() => _second.Received(1).Rehydrate();
}
