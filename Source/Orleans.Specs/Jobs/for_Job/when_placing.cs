// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Placement;

namespace Cratis.Orleans.Jobs.for_Job;

public class when_placing : Specification
{
    PlacementAttribute[] _attributes;

    void Because() => _attributes = [.. typeof(given.SomeJob).GetCustomAttributes(typeof(PlacementAttribute), inherit: true).Cast<PlacementAttribute>()];

    [Fact] void should_have_a_single_placement_strategy() => _attributes.Length.ShouldEqual(1);
    [Fact] void should_balance_by_resource_usage() => _attributes[0].ShouldBeOfExactType<ResourceOptimizedPlacementAttribute>();
}
