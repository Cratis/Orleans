// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Placement;

namespace Cratis.Orleans.Hosting.for_JobPlacementSiloBuilderExtensions;

public class when_adding_both_placements : given.a_silo_builder
{
    void Because()
    {
        _silo.AddJobPlacement<HostDirector>();
        _silo.AddJobStepPlacement();
    }

    [Fact] void should_use_the_host_director_for_controllers() => DirectorOf(typeof(JobsPlacementStrategy)).ShouldEqual(typeof(HostDirector));
    [Fact] void should_use_the_default_director_for_workers() => DirectorOf(typeof(JobStepsPlacementStrategy)).ShouldEqual(typeof(DefaultJobStepsPlacementDirector));
}
