// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Metadata;

namespace Cratis.Orleans.Jobs.Placement.given;

public class placement_properties : Specification
{
    protected Dictionary<string, string> _properties;

    void Establish() => _properties = [];

    protected static void Populate(IGrainPropertiesProvider provider, Type grainClass, Dictionary<string, string> properties) =>
        provider.Populate(grainClass, GrainType.Create("some-grain"), properties);

    protected bool HasPlacement(string strategy) =>
        _properties.TryGetValue(WellKnownGrainTypeProperties.PlacementStrategy, out var value) && value == strategy;
}
