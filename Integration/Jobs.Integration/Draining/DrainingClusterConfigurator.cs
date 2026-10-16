// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.Recovery;
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

namespace Cratis.Orleans.Jobs.Integration.Draining;

/// <summary>
/// Configures a cluster whose silos give a deactivating grain only a short time and count step state writes.
/// Jobs recovery is deliberately left out, so nothing but the step itself can activate a step again.
/// </summary>
public sealed class DrainingClusterConfigurator : ISiloConfigurator, IClientBuilderConfigurator
{
    /// <summary>
    /// How long a deactivating grain is given before Orleans abandons it.
    /// </summary>
    public static readonly TimeSpan DeactivationTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the signals the next cluster's silos share.
    /// </summary>
    public static DrainingSignals Signals { get; set; } = null!;

    /// <inheritdoc/>
    public void Configure(IConfiguration configuration, IClientBuilder clientBuilder) => clientBuilder.Services.AddCratisOrleansSerializers();

    /// <inheritdoc/>
    public void Configure(ISiloBuilder siloBuilder)
    {
        var signals = Signals;
        siloBuilder.Services.AddCratisOrleansSerializers();
        siloBuilder.Services.AddSingleton(signals);
        siloBuilder.Services.AddSingleton<ITypes>(_ => new Cratis.Types.Types());
        siloBuilder.Services.AddSingleton<IJobTypes, JobTypes>();
        siloBuilder.Services.AddSingleton<IJobs, Jobs>();
        siloBuilder.Services.AddSingleton<IJobStepThrottle, JobStepThrottle>();
        siloBuilder.Services.AddSingleton(Options.Create(new JobsOptions()));
        siloBuilder.Services.AddCratisOrleansMongoDBJobsStorage(
            new MongoClient(JobsClusterFixture.ConnectionString),
            options => options.DatabaseNameResolver = (_, _) => signals.Database);
        siloBuilder.Services.AddGrainStorage<JobGrainStorageProvider>(
            WellKnownGrainStorageProviders.Jobs,
            (provider, _) => new(provider.GetRequiredService<IJobsStorage>()));
        siloBuilder.Services.AddGrainStorage<CountingJobStepStorage>(
            WellKnownGrainStorageProviders.JobSteps,
            (provider, _) => new(new JobStepGrainStorageProvider(provider.GetRequiredService<IJobsStorage>()), signals));
        siloBuilder.AddPlacementDirector<RecoveryPlacement>(_ => new RecoveryPlacementDirector());
        siloBuilder.Services.AddSingleton<IGrainPropertiesProvider, RecoveryPlacementProperties>();
        siloBuilder.Configure<GrainCollectionOptions>(options => options.DeactivationTimeout = DeactivationTimeout);
    }
}
