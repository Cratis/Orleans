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

    /// <summary>
    /// Gets the silo that hosts what is activated after the victim dies. Never the primary: the test cluster's
    /// membership table lives on the primary, so restarting it would lose the membership and make every other
    /// silo terminate itself.
    /// </summary>
    public SiloAddress SurvivorAddress { get; }

    public RecoveryClusterFixture()
    {
        RecoveryClusterConfigurator.Signals = Signals;
        Cluster = new TestClusterBuilder(3)
            .AddSiloBuilderConfigurator<RecoveryClusterConfigurator>()
            .AddClientBuilderConfigurator<RecoveryClusterConfigurator>()
            .Build();
        Cluster.Deploy();
        Victim = Cluster.SecondarySilos[0];
        VictimAddress = AddressOf(Victim);
        SurvivorAddress = AddressOf(Cluster.SecondarySilos[1]);
        RecoveryPlacementDirector.Target = VictimAddress;
    }

    public IJobsManager Manager => Cluster.GrainFactory.GetJobsManager(Signals.Key.Scope, Signals.Key.Namespace);

    public async Task<(JobId Id, IRecoveryJob Job)> Start(bool throughLocalApi = false)
    {
        var id = JobId.New();
        var start = throughLocalApi
            ? ((InProcessSiloHandle)Cluster.Primary).SiloHost.Services.GetRequiredService<IJobs>()
                .Start<IRecoveryJob, RecoveryRequest>(new JobKey(Signals.Key.Scope, Signals.Key.Namespace), id, new())
            : Manager.Start<IRecoveryJob, RecoveryRequest>(id, new());
        var result = await start.WaitAsync(Deadline, TimeProvider.System);
        result.IsSuccess.ShouldBeTrue();
        var job = Cluster.GrainFactory.GetGrain<IRecoveryJob>(id, new JobKey(Signals.Key.Scope, Signals.Key.Namespace));
        (await job.Silo()).ShouldEqual(VictimAddress);
        (await Signals.FirstEntry.Task.WaitAsync(Deadline, TimeProvider.System)).ShouldEqual(VictimAddress);
        (await job.Inspect()).Status.ShouldEqual(JobStatus.Running);
        return (id, job);
    }

    public async Task Kill()
    {
        RecoveryPlacementDirector.Target = SurvivorAddress;
        await Cluster.KillSiloAsync(Victim).WaitAsync(Deadline, TimeProvider.System);
    }

    /// <summary>
    /// Gracefully restarts a silo and waits until the restarted silo has begun its own startup recovery pass.
    /// </summary>
    /// <param name="silo">The silo to restart.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="InvalidOperationException">The silo is the primary, which holds the membership table.</exception>
    public async Task Restart(SiloAddress silo)
    {
        var handle = Cluster.Silos.Single(candidate => AddressOf(candidate).Equals(silo));
        if (handle == Cluster.Primary)
        {
            throw new InvalidOperationException("The primary silo holds the test cluster's membership table; restarting it loses the cluster.");
        }
        var restarted = await Cluster.RestartSiloAsync(handle).WaitAsync(Deadline, TimeProvider.System);
        await Signals.RecoveryPassStartedOn(AddressOf(restarted!)).WaitAsync(Deadline, TimeProvider.System);
    }

    static SiloAddress AddressOf(SiloHandle silo) =>
        ((InProcessSiloHandle)silo).SiloHost.Services.GetRequiredService<ILocalSiloDetails>().SiloAddress;

    public void Dispose()
    {
        // Never release the original gate in the crash scenario, even during cleanup.
        Signals.ReleaseRecovered.TrySetResult();
        Cluster.StopAllSilos();
        Cluster.Dispose();
        new MongoClient(JobsClusterFixture.ConnectionString).DropDatabase(Signals.Database);
    }
}
