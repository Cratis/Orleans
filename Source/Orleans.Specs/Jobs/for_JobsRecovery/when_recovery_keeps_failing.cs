// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsRecovery;

public class when_recovery_keeps_failing : given.a_recovery_participant
{
    readonly List<TimeSpan> _delays = [];

    void Establish() => _keys.GetKeys(Arg.Any<CancellationToken>()).Returns(
        Task.FromException<IEnumerable<JobsManagerKey>>(new Exception("catalog unavailable")));

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
    [Fact] void should_keep_retrying_beyond_the_backoff_cap() => _keys.Received(9).GetKeys(Arg.Any<CancellationToken>());
}
