// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Runtime.Hosting;
using Orleans.Serialization.Configuration;

namespace Cratis.Orleans.Hosting;

/// <summary>
/// Extension methods for co-hosting an Orleans silo with the Cratis job system.
/// </summary>
/// <remarks>
/// The co-hosted silo uses localhost clustering by default; set <see cref="CratisOrleansOptions.Clustering"/> to
/// <see cref="ClusteringMode.MongoDB"/> for durable cluster membership when running more than one instance.
/// </remarks>
public static class CratisOrleansSiloBuilderExtensions
{
    /// <summary>
    /// Adds the co-hosted Orleans silo with the Cratis job system and its grain storage.
    /// </summary>
    /// <param name="builder">The <see cref="WebApplicationBuilder"/> to configure.</param>
    /// <param name="options">The <see cref="CratisOrleansOptions"/> to use.</param>
    /// <returns>The same <see cref="WebApplicationBuilder"/> for chaining.</returns>
    /// <remarks>
    /// <para>
    /// <c lang="csharp">Orleans.Providers.MongoDB</c> has no Orleans 10 release - 9.5.0 is a 9.x binary - so only the parts of it
    /// proven safe on the 10.x runtime are used: cluster membership and grain storage, and nothing else.
    /// </para>
    /// <para>
    /// The reminder table is where that version skew bites. Pointing the 10.x <c lang="csharp">LocalReminderService</c> at the
    /// 9.x <c lang="csharp">MongoReminderTable</c> makes the reminder service stop responding: the first
    /// <c lang="csharp">RegisterOrUpdateReminder</c> never completes, the calling grain's request times out, the reminder row is
    /// never written, and no reminder-backed grain ever ticks. Orleans' own in-memory reminder service is 10.x end
    /// to end and works in both modes, so it is used unconditionally. Reminders then do not survive a full cluster
    /// restart - which costs nothing for the job system, whose jobs rehydrate themselves from durable state on
    /// every start anyway.
    /// </para>
    /// </remarks>
    public static WebApplicationBuilder AddCratisOrleans(this WebApplicationBuilder builder, CratisOrleansOptions? options = default)
    {
        options ??= new CratisOrleansOptions();
        if (!options.Enabled)
        {
            return builder;
        }

        builder.Host.UseOrleans(silo =>
        {
            silo.Configure<ClusterOptions>(clusterOptions =>
            {
                clusterOptions.ClusterId = options.ClusterId;
                clusterOptions.ServiceId = options.ServiceId;
            });

            // Serializable types in client-facing assemblies can make Orleans' configuration analyzer fail at
            // startup on types this silo never serializes. Skip the analysis unless it is asked for.
            silo.Services.Configure<TypeManifestOptions>(typeManifestOptions => typeManifestOptions.EnableConfigurationAnalysis = false);

            silo.UseMongoDBClient(options.ConnectionString);

            if (options.Clustering == ClusteringMode.MongoDB)
            {
                silo.UseMongoDBClustering(membershipOptions =>
                {
                    membershipOptions.DatabaseName = options.DatabaseName;
                    membershipOptions.Strategy = MongoDBMembershipStrategy.SingleDocument;
                });
            }
            else
            {
                silo.UseLocalhostClustering();
            }

            // Durable grain state for the job system - a running job survives a silo going away mid-flight.
            silo.AddMongoDBGrainStorage(WellKnownGrainStorageProviders.Jobs, grainStorageOptions => grainStorageOptions.DatabaseName = options.DatabaseName);
            silo.AddMongoDBGrainStorage(WellKnownGrainStorageProviders.JobSteps, grainStorageOptions => grainStorageOptions.DatabaseName = options.DatabaseName);

            // Never the 9.x MongoDB reminder table on the 10.x runtime - it hangs the reminder service outright
            // (see the remarks on this method).
            silo.UseInMemoryReminderService();

            // The job system's grain storage providers resolve jobs storage per scope and namespace through
            // IJobsStorage - the MongoDB jobs storage package registers that. Register the providers under the
            // names the job grains ask for by name.
            silo.Services.AddGrainStorage<Jobs.JobGrainStorageProvider>(WellKnownGrainStorageProviders.Jobs, (serviceProvider, _) =>
                new(serviceProvider.GetRequiredService<IJobsStorage>()));
            silo.Services.AddGrainStorage<Jobs.JobStepGrainStorageProvider>(WellKnownGrainStorageProviders.JobSteps, (serviceProvider, _) =>
                new(serviceProvider.GetRequiredService<IJobsStorage>()));
        });

        return builder;
    }
}
