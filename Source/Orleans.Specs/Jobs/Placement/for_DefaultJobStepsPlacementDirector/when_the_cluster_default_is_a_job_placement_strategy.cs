// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Orleans.Jobs.Placement.for_DefaultJobStepsPlacementDirector;

public class when_the_cluster_default_is_a_job_placement_strategy : given.a_placement_target
{
    DefaultJobStepsPlacementDirector _director;
    Exception _result;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddSingleton<PlacementStrategy>(new JobStepsPlacementStrategy());
        _director = new(services.BuildServiceProvider());
    }

    async Task Because() => _result = await Catch.Exception(() => _director.OnAddActivation(new JobStepsPlacementStrategy(), _target, _context));

    [Fact] void should_refuse_to_delegate_to_itself() => _result.ShouldBeOfExactType<JobStepPlacementCannotUseItselfAsDefault>();
}
