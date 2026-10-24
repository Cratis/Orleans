// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Placement;

namespace Cratis.Orleans.Hosting.for_JobPlacementSiloBuilderExtensions;

public class when_adding_job_step_placement_twice : given.a_silo_builder
{
    void Because()
    {
        _silo.AddJobStepPlacement();
        _silo.AddJobStepPlacement();
        _silo.AddJobStepPlacement<HostDirector>();
        _silo.AddJobStepPlacement<HostDirector>();
    }

    [Fact] void should_register_the_properties_once() => Providers<JobStepsPlacementProperties>().ShouldEqual(1);
    [Fact] void should_register_the_strategy_once() => Strategies(nameof(JobStepsPlacementStrategy)).ShouldEqual(1);
    [Fact] void should_register_the_director_once() => Directors(typeof(JobStepsPlacementStrategy)).Count().ShouldEqual(1);
}
