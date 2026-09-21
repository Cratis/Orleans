// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Tenancy;

/// <summary>
/// Defines how the hosting application carries its tenant context across grain calls.
/// </summary>
/// <remarks>
/// <para>
/// The job system spawns grains whose keys durably name the scope and namespace they were started
/// for, but a grain call itself carries no request - a host that resolves tenant-scoped services
/// inside a job would otherwise silently bind them to whatever its request-bound tenant resolution
/// falls back to. Implementing this interface and registering it with
/// <see cref="TenancySiloBuilderExtensions.AddTenancy{TTenancy}"/> is what lets the tenant call
/// filters re-establish the host's tenant context automatically for every grain call that can name
/// a tenant.
/// </para>
/// <para>
/// The engine treats scope and namespace as opaque - Chronicle uses the event store name as the
/// scope and the tenant's namespace within it; another host might use the tenant directly. Only the
/// host knows the mapping, which is why it lives here.
/// </para>
/// </remarks>
public interface ITenancy
{
    /// <summary>
    /// Gets the tenant the current logical flow definitely acts for, or <see langword="null"/> when none is established.
    /// </summary>
    /// <remarks>
    /// This is what the outgoing filter records onto a grain call. Answer only a tenant the flow
    /// actually knows - an ambient tenant a background flow established, or the tenant of the
    /// request in flight. Never answer a deployment-wide fallback: recording a guess onto the call
    /// would promote it to knowledge on the receiving side.
    /// </remarks>
    string? Current { get; }

    /// <summary>
    /// Gets the tenant whose work a job's coordinates hold.
    /// </summary>
    /// <param name="scope">The storage scope from the job family grain key.</param>
    /// <param name="namespace">The namespace within the scope from the job family grain key.</param>
    /// <returns>The tenant, or <see langword="null"/> when the coordinates name no tenant.</returns>
    string? TenantFor(string scope, string @namespace);

    /// <summary>
    /// Establishes the tenant for the current logical flow until the returned scope is disposed.
    /// </summary>
    /// <param name="tenant">The tenant to establish.</param>
    /// <returns>A scope that restores the previous tenant context when disposed.</returns>
    IDisposable Establish(string tenant);
}
