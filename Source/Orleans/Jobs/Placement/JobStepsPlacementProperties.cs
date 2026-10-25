// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Metadata;

namespace Cratis.Orleans.Jobs.Placement;

/// <summary>
/// Represents the <see cref="IGrainPropertiesProvider"/> that gives every concrete job step the <see cref="JobStepsPlacementStrategy"/>.
/// </summary>
/// <remarks>
/// Job steps have no placement attribute of their own and use the cluster default, so a host that wants to place
/// workers itself overrides the final grain manifest instead of changing any step class. It is registered only when
/// a host opts in, so a cluster that does not opt in has an unchanged manifest.
/// </remarks>
public sealed class JobStepsPlacementProperties : IGrainPropertiesProvider
{
    /// <inheritdoc/>
    public void Populate(Type grainClass, GrainType grainType, Dictionary<string, string> properties)
    {
        if (JobsPlacementProperties.DerivesFromGeneric(grainClass, typeof(JobStep<,,>)))
        {
            properties[WellKnownGrainTypeProperties.PlacementStrategy] = nameof(JobStepsPlacementStrategy);
        }
    }
}
