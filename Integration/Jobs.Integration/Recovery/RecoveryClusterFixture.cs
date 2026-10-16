// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Orleans.TestingHost;

namespace Cratis.Orleans.Jobs.Integration.Recovery;

public sealed class RecoveryClusterFixture : IDisposable
{
    public static readonly TimeSpan Deadline = TimeSpan.FromSeconds(45);
    public TestCluster Cluster { get; }
    public RecoverySignals Signals { get; } = new($"orleans-recovery-{Guid.NewGuid():N}");
    public SiloHandle Victim { get; }
    public SiloAddress VictimAddress { get; }

    public RecoveryClusterFixture()
    {
        RecoveryClusterConfigurator.Signals = Signals;
        Cluster = new TestClusterBuilder(3)
            .AddSiloBuilderConfigurator<RecoveryClusterConfigurator>()
            .AddClientBuilderConfigurator<RecoveryClusterConfigurator>()
            .Build();
        Cluster.Deploy();
        Victim = Cluster.SecondarySilos[0];
        VictimAddress = ((InProcessSiloHandle)Victim).SiloHost.Services.GetRequiredService<ILocalSiloDetails>().SiloAddress;
        RecoveryPlacementDirector.Target = VictimAddress;
    }

    public IJobsManager Manager => Cluster.GrainFactory.GetJobsManager(Signals.Key.Scope, Signals.Key.Namespace);

    public async Task<(JobId Id, IRecoveryJob Job)> Start()
    {
        var id = JobId.New();
        var result = await Manager.Start<IRecoveryJob, RecoveryRequest>(id, new()).WaitAsync(Deadline, TimeProvider.System);
        result.IsSuccess.ShouldBeTrue();
        var job = Cluster.GrainFactory.GetGrain<IRecoveryJob>(id, new JobKey(Signals.Key.Scope, Signals.Key.Namespace));
        (await job.Silo()).ShouldEqual(VictimAddress);
        (await Signals.FirstEntry.Task.WaitAsync(Deadline, TimeProvider.System)).ShouldEqual(VictimAddress);
        (await job.Inspect()).Status.ShouldEqual(JobStatus.Running);
        return (id, job);
    }

    public async Task Kill()
    {
        RecoveryPlacementDirector.Target = null;
        await Cluster.KillSiloAsync(Victim).WaitAsync(Deadline, TimeProvider.System);
    }

    public void Dispose()
    {
        // Never release the original gate in the crash scenario, even during cleanup.
        Signals.ReleaseRecovered.TrySetResult();
        Cluster.StopAllSilos();
        Cluster.Dispose();
        new MongoClient(JobsClusterFixture.ConnectionString).DropDatabase(Signals.Database);
    }
}
