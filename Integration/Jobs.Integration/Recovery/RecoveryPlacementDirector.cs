// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime.Placement;

namespace Cratis.Orleans.Jobs.Integration.Recovery;

public sealed class RecoveryPlacementDirector : IPlacementDirector
{
    public static SiloAddress? Target { get; set; }

    /// <summary>
    /// Gets or sets a silo for the grains of one type, overriding <see cref="Target"/> for them.
    /// </summary>
    public static (GrainType Type, SiloAddress Silo)? TypeTarget { get; set; }

    public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
    {
        var compatible = context.GetCompatibleSilos(target);
        var wanted = TypeTarget is { } typed && typed.Type == target.GrainIdentity.Type ? typed.Silo : Target;
        return Task.FromResult(compatible.FirstOrDefault(silo => silo.Equals(wanted)) ?? compatible[0]);
    }
}
