// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Orleans.Tenancy;

/// <summary>
/// Extension methods for carrying the host's tenant context across grain calls.
/// </summary>
public static class TenancySiloBuilderExtensions
{
    /// <summary>
    /// Adds the tenant call filters, carrying the host's tenant context across every grain call
    /// automatically - from the job family's durable grain keys first, and from the calling flow
    /// otherwise.
    /// </summary>
    /// <param name="silo">The <see cref="ISiloBuilder"/> to configure.</param>
    /// <typeparam name="TTenancy">The host's <see cref="ITenancy"/> implementation.</typeparam>
    /// <returns>The same <see cref="ISiloBuilder"/> for chaining.</returns>
    /// <remarks>
    /// Without this, a multi-tenant host resolving tenant-scoped services inside a job silently
    /// binds them to whatever its request-bound tenant resolution falls back to, while the job's
    /// explicit writes go to the namespace its key names - a silent split-brain that only shows
    /// once a second tenant exists.
    /// </remarks>
    public static ISiloBuilder AddTenancy<TTenancy>(this ISiloBuilder silo)
        where TTenancy : class, ITenancy
    {
        silo.Services.AddSingleton<ITenancy, TTenancy>();
        silo.AddIncomingGrainCallFilter<TenantIncomingCallFilter>();
        silo.AddOutgoingGrainCallFilter<TenantOutgoingCallFilter>();
        return silo;
    }
}
