// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Placement;

namespace Cratis.Orleans.Hosting.for_JobPlacementSiloBuilderExtensions;

public class when_adding_a_job_director_after_the_default : given.a_silo_builder
{
    void Because()
    {
        _silo.AddJobPlacement();
        _silo.AddJobPlacement<HostDirector>();
    }

    [Fact] void should_use_the_host_director() => DirectorOf(typeof(JobsPlacementStrategy)).ShouldEqual(typeof(HostDirector));
    [Fact] void should_register_a_single_director() => Directors(typeof(JobsPlacementStrategy)).Count().ShouldEqual(1);
    [Fact] void should_register_a_single_strategy() => Strategies(nameof(JobsPlacementStrategy)).ShouldEqual(1);
}
