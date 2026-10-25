// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime.Placement;

namespace Cratis.Orleans.Jobs.Placement.given;

public class RecordingPlacementDirector(SiloAddress answer) : IPlacementDirector
{
    public List<(PlacementStrategy Strategy, PlacementTarget Target, IPlacementContext Context)> Calls { get; } = [];

    public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
    {
        Calls.Add((strategy, target, context));
        return Task.FromResult(answer);
    }
}
