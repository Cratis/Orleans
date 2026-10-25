// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Placement.for_JobStepsPlacementProperties;

public class when_populating_for_a_concrete_step : given.placement_properties
{
    void Because() => Populate(new JobStepsPlacementProperties(), typeof(given.SomeConcreteStep), _properties);

    [Fact] void should_choose_the_step_placement_strategy() => HasPlacement(nameof(JobStepsPlacementStrategy)).ShouldBeTrue();
}
