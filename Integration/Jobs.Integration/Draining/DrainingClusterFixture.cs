// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Recovery;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Orleans.TestingHost;

namespace Cratis.Orleans.Jobs.Integration.Draining;

/// <summary>
/// A two-silo cluster with a draining job and its step placed on the secondary silo.
/// </summary>
public sealed class DrainingClusterFixture : IDisposable
{
    /// <summary>
    /// The deadline for every wait in the specs.
    /// </summary>
    public static readonly TimeSpan Deadline = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Initializes a new instance of the <see cref="DrainingClusterFixture"/> class.
    /// </summary>
    public DrainingClusterFixture()
    {
        DrainingClusterConfigurator.Signals = Signals;
        Cluster = new TestClusterBuilder(2)
            .AddSiloBuilderConfigurator<DrainingClusterConfigurator>()
            .AddClientBuilderConfigurator<DrainingClusterConfigurator>()
            .Build();
        Cluster.Deploy();
        Host = Cluster.SecondarySilos[0];
        HostAddress = ((InProcessSiloHandle)Host).SiloHost.Services.GetRequiredService<ILocalSiloDetails>().SiloAddress;
        RecoveryPlacementDirector.Target = HostAddress;
    }

    /// <summary>
    /// Gets the cluster.
    /// </summary>
    public TestCluster Cluster { get; }

    /// <summary>
    /// Gets the signals the cluster's silos share.
    /// </summary>
    public DrainingSignals Signals { get; } = new($"orleans-draining-{Guid.NewGuid():N}");

    /// <summary>
    /// Gets the silo hosting the job and its step.
    /// </summary>
    public SiloHandle Host { get; }

    /// <summary>
    /// Gets the address of the silo hosting the job and its step.
    /// </summary>
    public SiloAddress HostAddress { get; }

    /// <summary>
    /// Gets the jobs manager.
    /// </summary>
    public IJobsManager Manager => Cluster.GrainFactory.GetJobsManager(Signals.Key.Scope, Signals.Key.Namespace);

    Cratis.Orleans.Storage.JobsStorage Storage => ((InProcessSiloHandle)Cluster.Primary).SiloHost.Services
        .GetRequiredService<Cratis.Orleans.Storage.IJobsStorage>()
        .GetFor(Signals.Key.Scope, Signals.Key.Namespace);

    /// <summary>
    /// Starts the job and waits until its step performs work on the hosting silo.
    /// </summary>
    /// <returns>The id of the started job.</returns>
    public async Task<JobId> StartAndWaitForTheStepToPerform()
    {
        var id = JobId.New();
        var result = await Manager.Start<IDrainingJob, DrainingRequest>(id, new()).WaitAsync(Deadline, TimeProvider.System);
        result.IsSuccess.ShouldBeTrue();
        (await Signals.Entered.Task.WaitAsync(Deadline, TimeProvider.System)).ShouldEqual(HostAddress);
        return id;
    }

    /// <summary>
    /// Gracefully stops the silo hosting the job and its step, letting anything activated afterwards go to the
    /// surviving silo.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public async Task StopHost()
    {
        RecoveryPlacementDirector.Target = null;
        await Cluster.StopSiloAsync(Host).WaitAsync(Deadline, TimeProvider.System);
    }

    /// <summary>
    /// Gets the persisted state of the job's only step.
    /// </summary>
    /// <param name="jobId">The job.</param>
    /// <returns>The <see cref="JobStepState"/>.</returns>
    public async Task<JobStepState> GetStepState(JobId jobId) => (await Storage.JobSteps.GetForJob(jobId)).AsT0.Single();

    /// <summary>
    /// Gets the job's only step grain.
    /// </summary>
    /// <param name="jobId">The job.</param>
    /// <returns>The <see cref="IDrainingStep"/>.</returns>
    public async Task<IDrainingStep> GetStep(JobId jobId)
    {
        var state = await GetStepState(jobId);
        return Cluster.GrainFactory.GetGrain<IDrainingStep>(state.Id.JobStepId, keyExtension: new JobStepKey(jobId, Signals.Key.Scope, Signals.Key.Namespace));
    }

    /// <summary>
    /// Gets the job grain.
    /// </summary>
    /// <param name="jobId">The job.</param>
    /// <returns>The <see cref="IDrainingJob"/>.</returns>
    public IDrainingJob GetJob(JobId jobId) =>
        Cluster.GrainFactory.GetGrain<IDrainingJob>(jobId, keyExtension: new JobKey(Signals.Key.Scope, Signals.Key.Namespace));

    /// <inheritdoc/>
    public void Dispose()
    {
        RecoveryPlacementDirector.Target = null;
        Cluster.StopAllSilos();
        Cluster.Dispose();
        new MongoClient(JobsClusterFixture.ConnectionString).DropDatabase(Signals.Database);
    }
}
