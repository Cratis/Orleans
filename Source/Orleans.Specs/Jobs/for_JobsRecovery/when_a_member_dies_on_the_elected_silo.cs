// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsRecovery;

public class when_a_member_dies_on_the_elected_silo : given.a_recovery_participant
{
    async Task Establish() => await Start();
    async Task Because()
    {
        await Publish(SiloStatus.Dead);
        await Publish(SiloStatus.Dead);
    }

    [Fact] void should_rehydrate_each_key_for_the_death_once() => _first.Received(1).Rehydrate();
    [Fact] void should_rehydrate_the_other_namespace() => _second.Received(1).Rehydrate();
}
