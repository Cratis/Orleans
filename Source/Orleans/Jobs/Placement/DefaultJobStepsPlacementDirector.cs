// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime.Placement;

namespace Cratis.Orleans.Jobs.Placement;

/// <summary>
/// Represents the <see cref="IPlacementDirector"/> for <see cref="JobStepsPlacementStrategy"/> that places job step
/// workers exactly as they are placed without the opt-in: the way the cluster's default placement strategy does.
/// </summary>
/// <param name="services">The <see cref="IServiceProvider"/> to resolve the cluster default placement from.</param>
public sealed class DefaultJobStepsPlacementDirector(IServiceProvider services) : IPlacementDirector
{
    readonly Lazy<(PlacementStrategy Strategy, IPlacementDirector Director)> _clusterDefault = new(() => ResolveClusterDefault(services));

    /// <inheritdoc/>
    /// <exception cref="JobStepPlacementCannotUseItselfAsDefault">The cluster default placement strategy is one of the job placement strategies.</exception>
    public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
    {
        var (clusterDefault, director) = _clusterDefault.Value;
        return director.OnAddActivation(clusterDefault, target, context);
    }

    static (PlacementStrategy Strategy, IPlacementDirector Director) ResolveClusterDefault(IServiceProvider services)
    {
        var clusterDefault = services.GetRequiredService<PlacementStrategy>();
        if (clusterDefault is JobsPlacementStrategy or JobStepsPlacementStrategy)
        {
            throw new JobStepPlacementCannotUseItselfAsDefault(clusterDefault.GetType());
        }

        return (clusterDefault, services.GetRequiredKeyedService<IPlacementDirector>(clusterDefault.GetType()));
    }
}
