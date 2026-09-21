// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Orleans.Tenancy;

/// <summary>
/// Establishes the host's tenant context for every incoming grain call that can name a tenant, so
/// the work a grain does - and every scoped collaborator it resolves - runs for the tenant the call
/// is actually for.
/// </summary>
/// <remarks>
/// <para>
/// The job family's grain keys are the durable truth: a <see cref="JobsManagerKey"/>,
/// <see cref="JobKey"/> and <see cref="JobStepKey"/> all carry the scope and namespace the job
/// belongs to, and the key survives restarts and resumes - so a job resumed after a restart still
/// runs for the tenant it was started for, and the key wins over anything the caller sent. The
/// host's <see cref="ITenancy.TenantFor"/> maps the coordinates to its tenant.
/// </para>
/// <para>
/// For every other grain the tenant rides the <see cref="RequestContext"/> entry
/// <see cref="TenantRequestContext.Key"/> that <see cref="TenantOutgoingCallFilter"/> wrote on the
/// way out. A call that names no tenant either way is invoked exactly as before.
/// </para>
/// </remarks>
/// <param name="tenancy">The host's <see cref="ITenancy"/>.</param>
public class TenantIncomingCallFilter(ITenancy tenancy) : IIncomingGrainCallFilter
{
    /// <summary>
    /// Invokes the call with the host's tenant context established when the call names a tenant.
    /// </summary>
    /// <param name="context">The <see cref="IIncomingGrainCallContext"/>.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        var tenant = TenantForTarget(context.TargetContext) ?? RequestContext.Get(TenantRequestContext.Key) as string;
        if (tenant is null)
        {
            await context.Invoke();
            return;
        }

        using (tenancy.Establish(tenant))
        {
            await context.Invoke();
        }
    }

    string? TenantForTarget(IGrainContext target)
    {
        var grainId = target.GrainId;
        return target.GrainInstance switch
        {
            IJobsManager when grainId.TryGetIntegerKey(out _, out var key) && key is not null =>
                TenantFor(JobsManagerKey.Parse(key)),
            IJob when grainId.TryGetGuidKey(out _, out var key) && key is not null =>
                TenantFor(JobKey.Parse(key)),
            IJobStep when grainId.TryGetGuidKey(out _, out var key) && key is not null =>
                TenantFor(JobStepKey.Parse(key)),
            _ => null
        };
    }

    string? TenantFor(JobsManagerKey key) => tenancy.TenantFor(key.Scope, key.Namespace);

    string? TenantFor(JobKey key) => tenancy.TenantFor(key.Scope, key.Namespace);

    string? TenantFor(JobStepKey key) => tenancy.TenantFor(key.Scope, key.Namespace);
}
