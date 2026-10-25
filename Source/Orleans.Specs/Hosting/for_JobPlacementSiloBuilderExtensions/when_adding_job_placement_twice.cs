// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Placement;

namespace Cratis.Orleans.Hosting.for_JobPlacementSiloBuilderExtensions;

public class when_adding_job_placement_twice : given.a_silo_builder
{
    void Because()
    {
        _silo.AddJobPlacement();
        _silo.AddJobPlacement();
        _silo.AddJobPlacement<HostDirector>();
        _silo.AddJobPlacement<HostDirector>();
    }

    [Fact] void should_register_the_properties_once() => Providers<JobsPlacementProperties>().ShouldEqual(1);
    [Fact] void should_register_the_strategy_once() => Strategies(nameof(JobsPlacementStrategy)).ShouldEqual(1);
    [Fact] void should_register_the_director_once() => Directors(typeof(JobsPlacementStrategy)).Count().ShouldEqual(1);
}
