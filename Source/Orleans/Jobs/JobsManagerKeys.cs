// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.DependencyInjection;

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Remembers manager keys activated on this silo. Hosts requiring complete recovery provide an
/// additional <see cref="IJobsManagerKeys"/> implementation that enumerates their durable scopes and namespaces.
/// </summary>
[Singleton]
public class JobsManagerKeys : IJobsManagerKeys
{
    readonly ConcurrentDictionary<JobsManagerKey, byte> _keys = new();

    /// <inheritdoc/>
    public Task<IEnumerable<JobsManagerKey>> GetKeys(CancellationToken cancellationToken) =>
        Task.FromResult<IEnumerable<JobsManagerKey>>(_keys.Keys.ToArray());

    /// <summary>
    /// Records a manager key used on this silo.
    /// </summary>
    /// <param name="key">The manager key.</param>
    internal void Remember(JobsManagerKey key) => _keys.TryAdd(key, 0);
}
