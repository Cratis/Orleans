// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;

namespace Cratis.Orleans.Jobs.Integration.Placement;

[Singleton]
public sealed class PlacementManagerKeys(PlacementSignals signals) : IJobsManagerKeys
{
    public Task<IEnumerable<JobsManagerKey>> GetKeys(CancellationToken cancellationToken) =>
        Task.FromResult<IEnumerable<JobsManagerKey>>([signals.Key]);
}
