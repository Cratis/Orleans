// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Hosting;
using Cratis.Orleans.Setup;
using Cratis.Orleans.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Orleans.Configuration;
using Orleans.Metadata;
using Orleans.Runtime.Hosting;
using Orleans.TestingHost;

namespace Cratis.Orleans.Jobs.Integration.Recovery;

public sealed class RecoveryClusterConfigurator : ISiloConfigurator, IClientBuilderConfigurator
{
    public static RecoverySignals Signals { get; set; } = null!;

    public void Configure(IConfiguration configuration, IClientBuilder clientBuilder) => clientBuilder.Services.AddCratisOrleansSerializers();

    public void Configure(ISiloBuilder siloBuilder)
    {
        var signals = Signals;
        siloBuilder.Services.AddCratisOrleansSerializers();
        siloBuilder.Services.AddSingleton(signals);
        siloBuilder.Services.AddSingleton<RecoveryManagerKeys>();
        siloBuilder.Services.AddSingleton<ITypes>(_ => new Cratis.Types.Types());
        siloBuilder.Services.AddSingleton<IJobTypes, JobTypes>();
        siloBuilder.Services.AddSingleton<IJobStepThrottle, JobStepThrottle>();
        siloBuilder.Services.AddSingleton(Options.Create(new JobsOptions()));
        siloBuilder.Services.AddCratisOrleansMongoDBJobsStorage(new MongoClient(JobsClusterFixture.ConnectionString),
            options => options.DatabaseNameResolver = (_, _) => signals.Database);
        siloBuilder.Services.AddGrainStorage<JobGrainStorageProvider>(WellKnownGrainStorageProviders.Jobs,
            (provider, _) => new(provider.GetRequiredService<IJobsStorage>()));
        siloBuilder.Services.AddGrainStorage<JobStepGrainStorageProvider>(WellKnownGrainStorageProviders.JobSteps,
            (provider, _) => new(provider.GetRequiredService<IJobsStorage>()));
        siloBuilder.AddPlacementDirector<RecoveryPlacement>(_ => new RecoveryPlacementDirector());
        siloBuilder.Services.AddSingleton<IGrainPropertiesProvider, RecoveryPlacementProperties>();
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
