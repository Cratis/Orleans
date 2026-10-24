// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Placement.for_JobStepsPlacementProperties;

public class when_populating_for_an_unrelated_grain : given.placement_properties
{
    void Because()
    {
        Populate(new JobStepsPlacementProperties(), typeof(JobsManager), _properties);
        Populate(new JobStepsPlacementProperties(), typeof(object), _properties);
    }

    [Fact] void should_not_add_any_property() => _properties.ShouldBeEmpty();
}
