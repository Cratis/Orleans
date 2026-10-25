// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Placement;

namespace Cratis.Orleans.Hosting.for_JobPlacementSiloBuilderExtensions;

public class when_adding_job_step_placement_only : given.a_silo_builder
{
    void Because() => _silo.AddJobStepPlacement();

    [Fact] void should_register_the_worker_strategy() => Strategies(nameof(JobStepsPlacementStrategy)).ShouldEqual(1);
    [Fact] void should_register_the_default_worker_director() => DirectorOf(typeof(JobStepsPlacementStrategy)).ShouldEqual(typeof(DefaultJobStepsPlacementDirector));
    [Fact] void should_register_the_worker_properties() => Providers<JobStepsPlacementProperties>().ShouldEqual(1);
    [Fact] void should_not_register_the_controller_strategy() => Strategies(nameof(JobsPlacementStrategy)).ShouldEqual(0);
    [Fact] void should_not_register_a_controller_director() => Directors(typeof(JobsPlacementStrategy)).ShouldBeEmpty();
    [Fact] void should_not_register_the_controller_properties() => Providers<JobsPlacementProperties>().ShouldEqual(0);
}
