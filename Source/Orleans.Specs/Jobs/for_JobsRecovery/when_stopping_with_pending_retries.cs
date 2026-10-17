// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsRecovery;

public class when_stopping_with_pending_retries : given.a_recovery_participant
{
    void Establish() => _keys.GetKeys(Arg.Any<CancellationToken>()).Returns(
        Task.FromException<IEnumerable<JobsManagerKey>>(new Exception("catalog unavailable")));

    async Task Because()
    {
        await _observer.OnStart(CancellationToken.None);
        var delay = await _timeProvider.NextDelay(Deadline);
        await _observer.OnStop(CancellationToken.None).WaitAsync(Deadline, TimeProvider.System);
        delay.Elapse();
    }

    [Fact] void should_not_call_the_provider_again_after_stop_completes() => _keys.Received(1).GetKeys(Arg.Any<CancellationToken>());
}
