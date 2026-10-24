// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Placement.for_JobsPlacementProperties;

public class when_populating_for_a_concrete_job : given.placement_properties
{
    void Because() => Populate(new JobsPlacementProperties(), typeof(for_Job.given.SomeJob), _properties);

    [Fact] void should_choose_the_job_placement_strategy() => HasPlacement(nameof(JobsPlacementStrategy)).ShouldBeTrue();
}
