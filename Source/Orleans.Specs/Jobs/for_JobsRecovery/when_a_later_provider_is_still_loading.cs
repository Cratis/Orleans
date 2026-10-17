// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobsRecovery;

public class when_a_later_provider_is_still_loading : given.a_recovery_participant
{
    readonly TaskCompletionSource<IEnumerable<JobsManagerKey>> _catalog = new(TaskCreationOptions.RunContinuationsAsynchronously);

    protected override IInstancesOf<IJobsManagerKeys> CreateProviders(IJobsManagerKeys keys)
    {
        var loading = Substitute.For<IJobsManagerKeys>();
        loading.GetKeys(Arg.Any<CancellationToken>()).Returns(_catalog.Task);
        return new KnownInstancesOf<IJobsManagerKeys>(keys, loading);
    }

    async Task Because()
    {
        await _observer.OnStart(CancellationToken.None);
        await _startup.Task.WaitAsync(Deadline, TimeProvider.System);
        await _observer.OnStop(CancellationToken.None).WaitAsync(Deadline, TimeProvider.System);
    }

    [Fact] void should_recover_the_first_providers_managers_without_waiting_for_later_catalogs() => _first.Received(1).Rehydrate();
    [Fact] void should_not_need_the_later_catalog_to_finish() => _catalog.Task.IsCompleted.ShouldBeFalse();
}
