// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Orleans.Jobs.for_JobsRecovery.given;

public class a_recovery_participant : Specification
{
    protected static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
    protected readonly SiloAddress _oldest = SiloAddress.New(new(System.Net.IPAddress.Loopback, 12000), 100);
    protected readonly SiloAddress _younger = SiloAddress.New(new(System.Net.IPAddress.Loopback, 12001), 200);
    protected readonly SiloAddress _victim = SiloAddress.New(new(System.Net.IPAddress.Loopback, 12002), 300);
    protected ILocalSiloDetails _local;
    protected IJobsManager _first;
    protected IJobsManager _second;
    protected JobsRecovery _recovery;
    protected ILifecycleObserver _observer;
    protected readonly TaskCompletionSource _startup = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly Channel<(ClusterMembershipSnapshot Snapshot, TaskCompletionSource Processed)> _updates = Channel.CreateUnbounded<(ClusterMembershipSnapshot, TaskCompletionSource)>();

    void Establish()
    {
        _local = Substitute.For<ILocalSiloDetails>();
        _local.SiloAddress.Returns(_oldest);
        var membership = Substitute.For<IClusterMembershipService>();
        membership.MembershipUpdates.Returns(Updates());
        var factory = Substitute.For<IGrainFactory>();
        _first = Substitute.For<IJobsManager>();
        _second = Substitute.For<IJobsManager>();
        factory.GetGrain<IJobsManager>(0, new JobsManagerKey("scope", "first")).Returns(_first);
        factory.GetGrain<IJobsManager>(0, new JobsManagerKey("scope", "second")).Returns(_second);
        _first.Rehydrate().Returns(Task.CompletedTask);
        _second.Rehydrate().Returns(_ =>
        {
            _startup.TrySetResult();
            return Task.CompletedTask;
        });
        var keys = Substitute.For<IJobsManagerKeys>();
        keys.GetKeys(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IEnumerable<JobsManagerKey>>(
            [new("scope", "first"), new("scope", "first"), new("scope", "second")]));
        _recovery = new(membership, _local, CreateProviders(keys), factory, NullLogger<JobsRecovery>.Instance);
        var lifecycle = Substitute.For<ISiloLifecycle>();
        lifecycle.Subscribe(Arg.Any<string>(), Arg.Any<int>(), Arg.Do<ILifecycleObserver>(observer => _observer = observer));
        _recovery.Participate(lifecycle);
    }

    protected virtual IInstancesOf<IJobsManagerKeys> CreateProviders(IJobsManagerKeys keys) => new KnownInstancesOf<IJobsManagerKeys>(keys);

    protected async Task Start()
    {
        await _observer.OnStart(CancellationToken.None);
        await _startup.Task.WaitAsync(Deadline, TimeProvider.System);

        // This also ensures startup's rehydrate has returned before assertions clear received calls.
        await Publish(SiloStatus.Active);
        _first.ClearReceivedCalls();
        _second.ClearReceivedCalls();
    }

    protected async Task Publish(SiloStatus victimStatus)
    {
        var members = new[]
        {
            new ClusterMember(_oldest, SiloStatus.Active, "oldest"),
            new ClusterMember(_younger, SiloStatus.Active, "younger"),
            new ClusterMember(_victim, victimStatus, "victim")
        };
        var snapshot = new ClusterMembershipSnapshot(members.ToImmutableDictionary(member => member.SiloAddress), new MembershipVersion(1));
        var processed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await _updates.Writer.WriteAsync((snapshot, processed));
        await processed.Task.WaitAsync(Deadline, TimeProvider.System);
    }

    async IAsyncEnumerable<ClusterMembershipSnapshot> Updates([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var (snapshot, processed) in _updates.Reader.ReadAllAsync(cancellationToken))
        {
            yield return snapshot;
            processed.TrySetResult();
        }
    }

    async Task Destroy()
    {
        await _observer.OnStop(CancellationToken.None).WaitAsync(Deadline, TimeProvider.System);
        _recovery.Dispose();
    }
}
