// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Provides the storage scopes and namespaces whose jobs should be recovered by this host.
/// </summary>
/// <remarks>
/// Implementations are discovered by convention. Provide all keys on every silo that can recover jobs;
/// the default only remembers managers activated on the current silo and cannot enumerate durable storage.
/// Implementations must be singletons, safe to resolve outside a request, and must name the scope and namespace
/// explicitly. Do not inject request-scoped or tenant-scoped dependencies; recovery has no request scope.
/// </remarks>
public interface IJobsManagerKeys
{
    /// <summary>
    /// Enumerates the manager keys to recover, including keys not yet used on this silo.
    /// </summary>
    /// <param name="cancellationToken">Cancellation for host shutdown.</param>
    /// <returns>The manager keys owned by the host.</returns>
    Task<IEnumerable<JobsManagerKey>> GetKeys(CancellationToken cancellationToken);
}
