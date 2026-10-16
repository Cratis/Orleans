// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Metadata;

namespace Cratis.Orleans.Jobs.Integration.Recovery;

public sealed class RecoveryPlacementProperties : IGrainPropertiesProvider
{
    public void Populate(Type grainClass, GrainType grainType, Dictionary<string, string> properties)
    {
        // The inherited ResourceOptimizedPlacement attribute otherwise wins over the derived attribute.
        // Override the final manifest for these test grains only, not the framework's placement policy.
        if (grainClass == typeof(RecoveryJob) || grainClass == typeof(RecoveryStep) ||
            grainClass == typeof(Draining.DrainingJob) || grainClass == typeof(Draining.DrainingStep))
        {
            properties[WellKnownGrainTypeProperties.PlacementStrategy] = nameof(RecoveryPlacement);
        }
    }
}
