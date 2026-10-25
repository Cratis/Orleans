// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime.Placement;

namespace Cratis.Orleans.Jobs.Placement.for_DefaultJobsPlacementDirector;

public class when_placing : given.a_placement_target
{
    given.RecordingPlacementDirector _resourceOptimized;
    PlacementStrategy _resourceOptimizedStrategy;
    DefaultJobsPlacementDirector _director;
    SiloAddress _result;

    void Establish()
    {
        _resourceOptimized = new(_answer);
        _resourceOptimizedStrategy = new ResourceOptimizedPlacement();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<PlacementStrategy>(nameof(ResourceOptimizedPlacement), _resourceOptimizedStrategy);
        services.AddKeyedSingleton<IPlacementDirector>(typeof(ResourceOptimizedPlacement), _resourceOptimized);
        _director = new(services.BuildServiceProvider());
    }

    async Task Because() => _result = await _director.OnAddActivation(new JobsPlacementStrategy(), _target, _context);

    [Fact] void should_return_the_answer_of_the_resource_optimized_director() => _result.ShouldEqual(_answer);
    [Fact] void should_ask_the_resource_optimized_director_once() => _resourceOptimized.Calls.Count.ShouldEqual(1);
    [Fact] void should_pass_the_resource_optimized_strategy_and_not_its_own() => _resourceOptimized.Calls[0].Strategy.ShouldBeSame(_resourceOptimizedStrategy);
    [Fact] void should_pass_the_target_through() => _resourceOptimized.Calls[0].Target.GrainIdentity.ShouldEqual(_target.GrainIdentity);
    [Fact] void should_pass_the_context_through() => _resourceOptimized.Calls[0].Context.ShouldBeSame(_context);
}
