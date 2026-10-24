// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime.Placement;

namespace Cratis.Orleans.Jobs.Placement.for_DefaultJobStepsPlacementDirector;

public class when_placing : given.a_placement_target
{
    given.RecordingPlacementDirector _clusterDefaultDirector;
    PlacementStrategy _clusterDefault;
    DefaultJobStepsPlacementDirector _director;
    SiloAddress _result;

    void Establish()
    {
        _clusterDefaultDirector = new(_answer);
        _clusterDefault = new ResourceOptimizedPlacement();
        var services = new ServiceCollection();
        services.AddSingleton<PlacementStrategy>(_clusterDefault);
        services.AddKeyedSingleton<IPlacementDirector>(typeof(ResourceOptimizedPlacement), _clusterDefaultDirector);
        _director = new(services.BuildServiceProvider());
    }

    async Task Because() => _result = await _director.OnAddActivation(new JobStepsPlacementStrategy(), _target, _context);

    [Fact] void should_return_the_answer_of_the_cluster_default_director() => _result.ShouldEqual(_answer);
    [Fact] void should_ask_the_cluster_default_director_once() => _clusterDefaultDirector.Calls.Count.ShouldEqual(1);
    [Fact] void should_pass_the_cluster_default_strategy_and_not_its_own() => _clusterDefaultDirector.Calls[0].Strategy.ShouldBeSame(_clusterDefault);
    [Fact] void should_pass_the_target_through() => _clusterDefaultDirector.Calls[0].Target.GrainIdentity.ShouldEqual(_target.GrainIdentity);
    [Fact] void should_pass_the_context_through() => _clusterDefaultDirector.Calls[0].Context.ShouldBeSame(_context);
}
