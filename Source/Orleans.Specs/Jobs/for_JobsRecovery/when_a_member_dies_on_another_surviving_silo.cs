// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsRecovery;

public class when_a_member_dies_on_another_surviving_silo : given.a_recovery_participant
{
    async Task Establish()
    {
        _local.SiloAddress.Returns(_younger);
        await Start();
    }
    async Task Because() => await Publish(SiloStatus.Dead);

    [Fact] void should_leave_recovery_to_the_oldest_active_silo() => _first.DidNotReceive().Rehydrate();
    [Fact] void should_not_rehydrate_another_namespace() => _second.DidNotReceive().Rehydrate();
}
