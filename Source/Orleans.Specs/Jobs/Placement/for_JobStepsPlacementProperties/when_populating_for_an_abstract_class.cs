// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs.Placement.for_JobStepsPlacementProperties;

public class when_populating_for_an_abstract_class : given.placement_properties
{
    void Because() => Populate(new JobStepsPlacementProperties(), typeof(JobStep<for_Job.given.SomeRequest, string, JobStepState>), _properties);

    [Fact] void should_not_add_any_property() => _properties.ShouldBeEmpty();
}
