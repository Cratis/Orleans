// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Hosting;
using Cratis.Orleans.Jobs.Integration.Recovery;
using Cratis.Orleans.Setup;
using Cratis.Orleans.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Orleans.Configuration;
using Orleans.Runtime.Hosting;
using Orleans.Runtime.MembershipService.SiloMetadata;
using Orleans.TestingHost;

namespace Cratis.Orleans.Jobs.Integration.Placement;

public sealed class PlacementClusterConfigurator : ISiloConfigurator, IClientBuilderConfigurator
{
    public static PlacementSignals Signals { get; set; } = null!;

    public void Configure(IConfiguration configuration, IClientBuilder clientBuilder) => clientBuilder.Services.AddCratisOrleansSerializers();

    public void Configure(ISiloBuilder siloBuilder)
    {
        var signals = Signals;
        var siloName = siloBuilder.Configuration["Orleans:Name"]
            ?? throw new InvalidOperationException("The test silo has no configured name.");
        var plan = signals.PlanFor(siloName);

        siloBuilder.Services.AddCratisOrleansSerializers();
        siloBuilder.Services.AddSingleton(signals);

        // The recovery integration fixtures share this assembly, so its keys are type-discovered here as well.
        siloBuilder.Services.AddSingleton(new RecoverySignals(signals.Database));
        siloBuilder.Services.AddSingleton<PlacementManagerKeys>();
        siloBuilder.Services.AddSingleton<ITypes>(_ => new Cratis.Types.Types());
        siloBuilder.Services.AddSingleton<IJobTypes, JobTypes>();
        siloBuilder.Services.AddSingleton<IJobs, Jobs>();
        siloBuilder.Services.AddSingleton<IJobStepThrottle, JobStepThrottle>();
        siloBuilder.Services.AddSingleton(Options.Create(new JobsOptions { JobWatchdogInterval = TimeSpan.FromSeconds(1) }));
        siloBuilder.Services.AddCratisOrleansMongoDBJobsStorage(new MongoClient(JobsClusterFixture.ConnectionString),
            options => options.DatabaseNameResolver = (_, _) => signals.Database);
        siloBuilder.Services.AddGrainStorage<JobGrainStorageProvider>(WellKnownGrainStorageProviders.Jobs,
            (provider, _) => new(provider.GetRequiredService<IJobsStorage>()));
        siloBuilder.Services.AddGrainStorage<JobStepGrainStorageProvider>(WellKnownGrainStorageProviders.JobSteps,
            (provider, _) => new(provider.GetRequiredService<IJobsStorage>()));
        siloBuilder.UseSiloMetadata(plan.ToMetadata());
        siloBuilder.AddJobPlacement<TestControllerDirector>();
        siloBuilder.AddJobStepPlacement<TestWorkerDirector>();
        siloBuilder.UseJobStepParallelismFromSiloMetadata();
        siloBuilder.AddCratisOrleansJobsRecovery();
        siloBuilder.Configure<ClusterMembershipOptions>(options =>
        {
            options.ProbeTimeout = TimeSpan.FromSeconds(1);
            options.NumMissedProbesLimit = 2;
            options.NumVotesForDeathDeclaration = 1;
            options.TableRefreshTimeout = TimeSpan.FromSeconds(1);
            options.DeathVoteExpirationTimeout = TimeSpan.FromSeconds(10);
        });
    }
}
