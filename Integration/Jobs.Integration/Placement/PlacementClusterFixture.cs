// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Orleans.Runtime.MembershipService.SiloMetadata;
using Orleans.TestingHost;

namespace Cratis.Orleans.Jobs.Integration.Placement;

public sealed class PlacementClusterFixture : IDisposable
{
    public static readonly TimeSpan Deadline = TimeSpan.FromSeconds(45);

    public TestCluster Cluster { get; }
    public PlacementSignals Signals { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlacementClusterFixture"/> class.
    /// </summary>
    /// <param name="plans">What each initial silo is for, by silo name (Primary, Secondary_1 and so on).</param>
    /// <param name="extras">What silos started later are for, in start order.</param>
    /// <param name="roundRobinWorkers">Whether workers are chosen round robin rather than by preference.</param>
    public PlacementClusterFixture(IReadOnlyDictionary<string, SiloPlan> plans, IEnumerable<SiloPlan>? extras = null, bool roundRobinWorkers = false)
    {
        Signals = new($"orleans-placement-{Guid.NewGuid():N}", plans, extras, roundRobinWorkers);
        PlacementClusterConfigurator.Signals = Signals;
        Cluster = new TestClusterBuilder((short)plans.Count)
            .AddSiloBuilderConfigurator<PlacementClusterConfigurator>()
            .AddClientBuilderConfigurator<PlacementClusterConfigurator>()
            .Build();
        Cluster.Deploy();
    }

    public IJobsManager Manager => Cluster.GrainFactory.GetJobsManager(Signals.Key.Scope, Signals.Key.Namespace);

    public SiloAddress AddressOf(string name) => AddressOf(HandleOf(name));

    public IPlacementJob JobOf(JobId id) =>
        Cluster.GrainFactory.GetGrain<IPlacementJob>(id, new JobKey(Signals.Key.Scope, Signals.Key.Namespace));

    public async Task WaitUntilMetadataIsVisible() =>
        await Eventually(() =>
        {
            var silos = Cluster.Silos.ToList();
            var addresses = silos.ConvertAll(silo => AddressOf(silo));
            return silos.TrueForAll(silo =>
            {
                var cache = ((InProcessSiloHandle)silo).SiloHost.Services.GetRequiredService<ISiloMetadataCache>();
                return addresses.TrueForAll(address => cache.GetSiloMetadata(address).Metadata.ContainsKey(SiloPlan.RoleKey));
            });
        });

    public async Task<(JobId Id, bool Succeeded)> TryStart(int steps)
    {
        var id = JobId.New();
        var result = await Manager.Start<IPlacementJob, PlacementRequest>(id, new(steps)).WaitAsync(Deadline, TimeProvider.System);
        return (id, result.IsSuccess);
    }

    public async Task<IPlacementJob> Start(int steps)
    {
        var (id, succeeded) = await TryStart(steps);
        succeeded.ShouldBeTrue();
        return JobOf(id);
    }

    public async Task Kill(string name)
    {
        var handle = HandleOf(name);
        Signals.MarkDead(AddressOf(handle));
        await Cluster.KillSiloAsync(handle).WaitAsync(Deadline, TimeProvider.System);
    }

    public async Task<SiloAddress> StartWorker()
    {
        var handle = await Cluster.StartAdditionalSiloAsync().WaitAsync(Deadline, TimeProvider.System);
        return AddressOf(handle);
    }

    public async Task Eventually(Func<bool> condition)
    {
        var until = DateTime.UtcNow + Deadline;
        while (!condition())
        {
            if (DateTime.UtcNow > until)
            {
                throw new TimeoutException("The expected condition never became true.");
            }
            await Task.Delay(50);
        }
    }

    public void Dispose()
    {
        Signals.ReleaseAll();
        Cluster.StopAllSilos();
        Cluster.Dispose();
        new MongoClient(JobsClusterFixture.ConnectionString).DropDatabase(Signals.Database);
    }

    static SiloAddress AddressOf(SiloHandle silo) =>
        ((InProcessSiloHandle)silo).SiloHost.Services.GetRequiredService<ILocalSiloDetails>().SiloAddress;

    SiloHandle HandleOf(string name) => Cluster.Silos.Single(silo => silo.Name == name);
}
