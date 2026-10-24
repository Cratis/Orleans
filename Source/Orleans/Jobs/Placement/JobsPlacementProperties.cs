// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Metadata;

namespace Cratis.Orleans.Jobs.Placement;

/// <summary>
/// Represents the <see cref="IGrainPropertiesProvider"/> that gives every concrete job the <see cref="JobsPlacementStrategy"/>.
/// </summary>
/// <remarks>
/// The <see cref="Job{TRequest, TJobState}"/> placement attribute is inherited, and it wins over an attribute on a
/// derived class. Overriding the final grain manifest is what lets a host choose placement without changing any
/// job class. It is registered only when a host opts in, so a cluster that does not opt in has an unchanged manifest.
/// </remarks>
public sealed class JobsPlacementProperties : IGrainPropertiesProvider
{
    /// <inheritdoc/>
    public void Populate(Type grainClass, GrainType grainType, Dictionary<string, string> properties)
    {
        if (DerivesFromGeneric(grainClass, typeof(Job<,>)))
        {
            properties[WellKnownGrainTypeProperties.PlacementStrategy] = nameof(JobsPlacementStrategy);
        }
    }

    /// <summary>
    /// Gets whether a grain class is concrete and derives from a generic base class.
    /// </summary>
    /// <param name="grainClass">The grain class.</param>
    /// <param name="openGenericBase">The open generic base class.</param>
    /// <returns>True if the class is not abstract and has the base class somewhere in its hierarchy, false otherwise.</returns>
    internal static bool DerivesFromGeneric(Type grainClass, Type openGenericBase)
    {
        if (grainClass.IsAbstract)
        {
            return false;
        }

        for (var type = grainClass.BaseType; type is not null; type = type.BaseType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == openGenericBase)
            {
                return true;
            }
        }

        return false;
    }
}
