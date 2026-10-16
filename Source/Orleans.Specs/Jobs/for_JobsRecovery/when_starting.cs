// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsRecovery;

public class when_starting : given.a_recovery_participant
{
    async Task Because()
    {
        await _observer.OnStart(CancellationToken.None);
        await _startup.Task.WaitAsync(Deadline, TimeProvider.System);
        await Publish(SiloStatus.Active);
    }

    [Fact] void should_rehydrate_the_first_namespace_once() => _first.Received(1).Rehydrate();
    [Fact] void should_rehydrate_the_second_namespace_once() => _second.Received(1).Rehydrate();
}
