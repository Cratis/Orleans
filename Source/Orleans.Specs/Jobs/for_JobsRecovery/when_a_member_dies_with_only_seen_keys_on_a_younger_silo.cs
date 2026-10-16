// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsRecovery;

public class when_a_member_dies_with_only_seen_keys_on_a_younger_silo : given.a_recovery_participant_with_seen_keys
{
    async Task Establish()
    {
        _local.SiloAddress.Returns(_younger);
        await Start();
    }

    async Task Because() => await Publish(SiloStatus.Dead);

    [Fact] void should_recover_its_own_first_namespace() => _first.Received(1).Rehydrate();
    [Fact] void should_recover_its_own_second_namespace() => _second.Received(1).Rehydrate();
}
