// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Placement.for_JobsPlacementProperties;

public class when_populating_for_an_abstract_class : given.placement_properties
{
    void Because() => Populate(new JobsPlacementProperties(), typeof(Job<for_Job.given.SomeRequest, for_Job.given.SomeJobState>), _properties);

    [Fact] void should_not_add_any_property() => _properties.ShouldBeEmpty();
}
