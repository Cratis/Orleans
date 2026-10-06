// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Orleans.Tenancy;

namespace Cratis.Orleans.Jobs.Integration.Hosting.for_CratisOrleansSiloBuilderExtensions;

public class TraceContextProbe : Grain, ITraceContextProbe
{
    public Task<string[]> Read(bool relay) => relay
        ? GrainFactory.GetGrain<ITraceContextProbe>(Guid.NewGuid()).Read(false)
        : Task.FromResult(new[]
        {
            Activity.Current?.TraceId.ToString() ?? string.Empty,
            RequestContext.Get(TenantRequestContext.Key) as string ?? string.Empty,
            Activity.Current?.GetBaggageItem(TenantRequestContext.Key) ?? string.Empty,
            Activity.Current?.GetBaggageItem("cratis.tenant") ?? string.Empty,
            Activity.Current?.GetBaggageItem("probe") ?? string.Empty
        });
}
