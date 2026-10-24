// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime.Placement;

namespace Cratis.Orleans.Jobs.Placement.for_DefaultJobStepsPlacementDirector;

public class when_the_host_overrides_the_cluster_default : given.a_placement_target
{
    given.RecordingPlacementDirector _hostDefaultDirector;
    given.RecordingPlacementDirector _otherDirector;
    PlacementStrategy _hostDefault;
    DefaultJobStepsPlacementDirector _director;

    void Establish()
    {
        _hostDefaultDirector = new(_answer);
        _otherDirector = new(_answer);
        _hostDefault = new given.SomeClusterDefaultPlacement();
        var services = new ServiceCollection();
        services.AddSingleton<PlacementStrategy>(_hostDefault);
        services.AddKeyedSingleton<IPlacementDirector>(typeof(given.SomeClusterDefaultPlacement), _hostDefaultDirector);
        services.AddKeyedSingleton<IPlacementDirector>(typeof(ResourceOptimizedPlacement), _otherDirector);
        _director = new(services.BuildServiceProvider());
    }

    Task Because() => _director.OnAddActivation(new JobStepsPlacementStrategy(), _target, _context);

    [Fact] void should_follow_the_host_default_director() => _hostDefaultDirector.Calls.Count.ShouldEqual(1);
    [Fact] void should_pass_the_host_default_strategy() => _hostDefaultDirector.Calls[0].Strategy.ShouldBeSame(_hostDefault);
    [Fact] void should_not_use_any_other_director() => _otherDirector.Calls.Count.ShouldEqual(0);
}
