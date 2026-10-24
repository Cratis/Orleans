// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Jobs.Placement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Metadata;
using Orleans.Runtime.MembershipService.SiloMetadata;
using Orleans.Runtime.Placement;

namespace Cratis.Orleans.Hosting;

/// <summary>
/// Extension methods for choosing where job controllers and job step workers are placed, and how many steps a silo runs.
/// </summary>
/// <remarks>
/// <para>
/// Controller placement (<see cref="AddJobPlacement(ISiloBuilder)"/>) and worker placement
/// (<see cref="AddJobStepPlacement(ISiloBuilder)"/>) are independent. Without either, nothing changes: controllers use
/// resource-optimized placement and workers use the cluster's default placement.
/// </para>
/// <para>
/// Enable the job or job-step placement strategy only once every silo in the cluster runs this version, and register it
/// identically on every silo. A silo without the registration cannot place a grain whose manifest names the strategy.
/// </para>
/// <para>
/// The opt-in replaces the placement attribute of every concrete job or job step class, including the
/// resource-optimized placement attribute that jobs carry, without changing any of those classes.
/// </para>
/// </remarks>
public static class JobPlacementSiloBuilderExtensions
{
    /// <summary>
    /// Places job controllers with the default job placement, which is resource-optimized placement, as without this call.
    /// </summary>
    /// <param name="silo">The silo builder.</param>
    /// <returns>The silo builder for chaining.</returns>
    /// <remarks>
    /// This only routes controllers through <see cref="JobsPlacementStrategy"/> so that a director can be registered later or
    /// on another call. A director registered with <see cref="AddJobPlacement{TDirector}(ISiloBuilder)"/> wins in either order.
    /// </remarks>
    public static ISiloBuilder AddJobPlacement(this ISiloBuilder silo) =>
        AddPlacement<JobsPlacementStrategy, DefaultJobsPlacementDirector, JobsPlacementProperties>(silo);

    /// <summary>
    /// Places job controllers with a host-supplied director.
    /// </summary>
    /// <typeparam name="TDirector">The <see cref="IPlacementDirector"/> that places the controllers, registered as a singleton.</typeparam>
    /// <param name="silo">The silo builder.</param>
    /// <returns>The silo builder for chaining.</returns>
    /// <remarks>
    /// A director must answer promptly and must not block. Register the same director on every silo.
    /// </remarks>
    public static ISiloBuilder AddJobPlacement<TDirector>(this ISiloBuilder silo)
        where TDirector : class, IPlacementDirector =>
        AddPlacement<JobsPlacementStrategy, TDirector, JobsPlacementProperties>(silo, replaceDirector: true);

    /// <summary>
    /// Places job step workers the way the cluster's default placement does, as without this call.
    /// </summary>
    /// <param name="silo">The silo builder.</param>
    /// <returns>The silo builder for chaining.</returns>
    /// <remarks>
    /// This only routes workers through <see cref="JobStepsPlacementStrategy"/> so that a director can be registered later or
    /// on another call. A director registered with <see cref="AddJobStepPlacement{TDirector}(ISiloBuilder)"/> wins in either order.
    /// </remarks>
    public static ISiloBuilder AddJobStepPlacement(this ISiloBuilder silo) =>
        AddPlacement<JobStepsPlacementStrategy, DefaultJobStepsPlacementDirector, JobStepsPlacementProperties>(silo);

    /// <summary>
    /// Places job step workers with a host-supplied director.
    /// </summary>
    /// <typeparam name="TDirector">The <see cref="IPlacementDirector"/> that places the workers, registered as a singleton.</typeparam>
    /// <param name="silo">The silo builder.</param>
    /// <returns>The silo builder for chaining.</returns>
    /// <remarks>
    /// Throw <see cref="NoEligibleJobWorker"/> when no silo is eligible. A director must answer promptly and must not block.
    /// Register the same director on every silo.
    /// </remarks>
    public static ISiloBuilder AddJobStepPlacement<TDirector>(this ISiloBuilder silo)
        where TDirector : class, IPlacementDirector =>
        AddPlacement<JobStepsPlacementStrategy, TDirector, JobStepsPlacementProperties>(silo, replaceDirector: true);

    /// <summary>
    /// Takes the number of job steps this silo runs in parallel from its silo metadata.
    /// </summary>
    /// <param name="silo">The silo builder.</param>
    /// <param name="metadataKey">The silo metadata key that holds the limit.</param>
    /// <returns>The silo builder for chaining.</returns>
    /// <remarks>
    /// Publish the value with Orleans' UseSiloMetadata. A silo without a usable value uses <see cref="JobsOptions.MaxParallelSteps"/>.
    /// It works in either order with <see cref="CratisOrleansSiloBuilderExtensions"/>.
    /// </remarks>
    public static ISiloBuilder UseJobStepParallelismFromSiloMetadata(this ISiloBuilder silo, string metadataKey = SiloMetadataJobStepParallelism.DefaultMetadataKey)
    {
        silo.Services.RemoveAll<IJobStepThrottle>();
        silo.Services.AddSingleton<IJobStepThrottle>(services =>
        {
            var parallelism = new SiloMetadataJobStepParallelism(
                services.GetRequiredService<IOptions<SiloMetadata>>(),
                services.GetRequiredService<IOptions<JobsOptions>>(),
                services.GetRequiredService<ILogger<SiloMetadataJobStepParallelism>>());
            return new JobStepThrottle(parallelism.GetMaxParallelSteps(metadataKey), services.GetRequiredService<ILogger<JobStepThrottle>>());
        });
        return silo;
    }

    static ISiloBuilder AddPlacement<TStrategy, TDirector, TProperties>(ISiloBuilder silo, bool replaceDirector = false)
        where TStrategy : PlacementStrategy, new()
        where TDirector : class, IPlacementDirector
        where TProperties : class, IGrainPropertiesProvider
    {
        var services = silo.Services;
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IGrainPropertiesProvider, TProperties>());

        AddStrategy<TStrategy>(services);
        AddDirector<TStrategy, TDirector>(services, replaceDirector);

        return silo;
    }

    static void AddStrategy<TStrategy>(IServiceCollection services)
        where TStrategy : PlacementStrategy, new()
    {
        var strategyKey = typeof(TStrategy).Name;
        if (!services.Any(descriptor => descriptor.ServiceType == typeof(PlacementStrategy) && Equals(descriptor.ServiceKey, strategyKey)))
        {
            services.Add(ServiceDescriptor.DescribeKeyed(typeof(PlacementStrategy), strategyKey, typeof(TStrategy), ServiceLifetime.Singleton));
        }
    }

    static void AddDirector<TStrategy, TDirector>(IServiceCollection services, bool replaceDirector)
        where TStrategy : PlacementStrategy
        where TDirector : class, IPlacementDirector
    {
        var directorKey = typeof(TStrategy);
        var directors = services.Where(descriptor => descriptor.ServiceType == typeof(IPlacementDirector) && Equals(descriptor.ServiceKey, directorKey)).ToList();
        if (replaceDirector)
        {
            foreach (var existing in directors)
            {
                services.Remove(existing);
            }
        }

        if (replaceDirector || directors.Count == 0)
        {
            services.AddKeyedSingleton<IPlacementDirector, TDirector>(directorKey);
        }
    }
}
