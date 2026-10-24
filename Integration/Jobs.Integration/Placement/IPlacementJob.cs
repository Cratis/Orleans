// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.Placement;

public interface IPlacementJob : IJob<PlacementRequest>
{
    Task<SiloAddress> Silo();
    Task<JobState> Inspect();
}

[GenerateSerializer]
public sealed record PlacementRequest([property: Id(0)] int Steps) : IJobRequest;
