// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Tenancy;

/// <summary>
/// Carries the caller's tenant onto every outgoing grain call, so a grain whose key does not name a
/// tenant still learns which tenant asked.
/// </summary>
/// <remarks>
/// Writes only the tenant the flow definitely acts for - <see cref="ITenancy.Current"/> answers
/// <see langword="null"/> for a flow that has none, and nothing is recorded for it. See the remarks
/// on <see cref="ITenancy.Current"/> for why a deployment-wide fallback must never be written.
/// </remarks>
/// <param name="tenancy">The host's <see cref="ITenancy"/>.</param>
public class TenantOutgoingCallFilter(ITenancy tenancy) : IOutgoingGrainCallFilter
{
    /// <summary>
    /// Invokes the call with the caller's tenant recorded in the <see cref="RequestContext"/> when the flow has one.
    /// </summary>
    /// <param name="context">The <see cref="IOutgoingGrainCallContext"/>.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Invoke(IOutgoingGrainCallContext context)
    {
        if (tenancy.Current is { } tenant)
        {
            RequestContext.Set(TenantRequestContext.Key, tenant);
        }

        await context.Invoke();
    }
}
