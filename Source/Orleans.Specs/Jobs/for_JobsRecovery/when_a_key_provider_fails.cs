// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsRecovery;

public class when_a_key_provider_fails : given.a_recovery_participant
{
    protected override IInstancesOf<IJobsManagerKeys> CreateProviders(IJobsManagerKeys keys)
    {
        var failed = Substitute.For<IJobsManagerKeys>();
        failed.GetKeys(Arg.Any<CancellationToken>()).Returns(Task.FromException<IEnumerable<JobsManagerKey>>(new Exception("catalog unavailable")));
        return new KnownInstancesOf<IJobsManagerKeys>(failed, keys);
    }

    async Task Because()
    {
        await Start();
        await Publish(SiloStatus.Dead);
    }

    [Fact] void should_continue_recovering_other_providers_after_startup() => _first.Received(1).Rehydrate();
    [Fact] void should_keep_listening_for_deaths() => _second.Received(1).Rehydrate();
}
