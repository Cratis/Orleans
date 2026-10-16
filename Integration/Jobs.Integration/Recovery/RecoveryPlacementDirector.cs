// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime.Placement;

namespace Cratis.Orleans.Jobs.Integration.Recovery;

public sealed class RecoveryPlacementDirector : IPlacementDirector
{
    public static SiloAddress? Target { get; set; }

    public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
    {
        var compatible = context.GetCompatibleSilos(target);
        return Task.FromResult(compatible.FirstOrDefault(silo => silo.Equals(Target)) ?? compatible[0]);
    }
}
