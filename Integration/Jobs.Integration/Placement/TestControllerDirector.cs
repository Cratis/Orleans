// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Placement;
using Orleans.Runtime.MembershipService.SiloMetadata;
using Orleans.Runtime.Placement;

namespace Cratis.Orleans.Jobs.Integration.Placement;

public sealed class TestControllerDirector(PlacementSignals signals, ISiloMetadataCache cache) : IPlacementDirector
{
    public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
    {
        Interlocked.Increment(ref signals.ControllerDirectorCalls);
        var eligible = SiloEligibility.WithRole(context.GetCompatibleSilos(target), cache, SiloPlan.ControllerRole);
        return eligible.Count == 0
            ? throw new NoEligibleJobWorker(target.GrainIdentity, "no silo is a job controller")
            : Task.FromResult(eligible[0]);
    }
}
