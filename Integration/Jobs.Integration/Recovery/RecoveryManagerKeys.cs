// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;

namespace Cratis.Orleans.Jobs.Integration.Recovery;

[Singleton]
public sealed class RecoveryManagerKeys(RecoverySignals signals, ILocalSiloDetails local) : IJobsManagerKeys
{
    public Task<IEnumerable<JobsManagerKey>> GetKeys(CancellationToken cancellationToken)
    {
        signals.RecoveryPassStarted(local.SiloAddress);
        return Task.FromResult<IEnumerable<JobsManagerKey>>([signals.Key]);
    }
}
