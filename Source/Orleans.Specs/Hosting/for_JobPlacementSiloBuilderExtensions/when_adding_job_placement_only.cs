// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Placement;

namespace Cratis.Orleans.Hosting.for_JobPlacementSiloBuilderExtensions;

public class when_adding_job_placement_only : given.a_silo_builder
{
    void Because() => _silo.AddJobPlacement();

    [Fact] void should_register_the_controller_strategy() => Strategies(nameof(JobsPlacementStrategy)).ShouldEqual(1);
    [Fact] void should_register_the_default_controller_director() => DirectorOf(typeof(JobsPlacementStrategy)).ShouldEqual(typeof(DefaultJobsPlacementDirector));
    [Fact] void should_register_the_controller_properties() => Providers<JobsPlacementProperties>().ShouldEqual(1);
    [Fact] void should_not_register_the_worker_strategy() => Strategies(nameof(JobStepsPlacementStrategy)).ShouldEqual(0);
    [Fact] void should_not_register_a_worker_director() => Directors(typeof(JobStepsPlacementStrategy)).ShouldBeEmpty();
    [Fact] void should_not_register_the_worker_properties() => Providers<JobStepsPlacementProperties>().ShouldEqual(0);
}
