// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime.Placement;

namespace Cratis.Orleans.Jobs.Placement;

/// <summary>
/// Represents the <see cref="IPlacementDirector"/> for <see cref="JobsPlacementStrategy"/> that places job controllers
/// exactly as they are placed without the opt-in: with resource-optimized placement.
/// </summary>
/// <remarks>
/// The resource-optimized director is internal to Orleans, so this one resolves it, and its strategy, from the
/// keyed services Orleans registers them under, and passes the call through unchanged.
/// </remarks>
/// <param name="services">The <see cref="IServiceProvider"/> to resolve Orleans' own placement director from.</param>
public sealed class DefaultJobsPlacementDirector(IServiceProvider services) : IPlacementDirector
{
    readonly Lazy<(PlacementStrategy Strategy, IPlacementDirector Director)> _resourceOptimized = new(() =>
        (services.GetRequiredKeyedService<PlacementStrategy>(nameof(ResourceOptimizedPlacement)),
         services.GetRequiredKeyedService<IPlacementDirector>(typeof(ResourceOptimizedPlacement))));

    /// <inheritdoc/>
    public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
    {
        var (resourceOptimized, director) = _resourceOptimized.Value;
        return director.OnAddActivation(resourceOptimized, target, context);
    }
}
