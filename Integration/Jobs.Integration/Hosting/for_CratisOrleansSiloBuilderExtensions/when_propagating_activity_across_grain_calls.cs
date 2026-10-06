// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Orleans.Tenancy;

namespace Cratis.Orleans.Jobs.Integration.Hosting.for_CratisOrleansSiloBuilderExtensions;

[Collection(JobsClusterCollection.Name)]
public class when_propagating_activity_across_grain_calls : Specification
{
    JobsClusterFixture _fixture;
    ActivityListener _listener;
    ActivitySource _source;
    Activity _request;
    string[] _context;

    void Establish()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = (ref _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(_listener);
        _source = new ActivitySource("Cratis.Orleans.Propagation.Specs");
        _fixture = JobsClusterFixture.Shared;
    }

    async Task Because()
    {
        _request = _source.StartActivity("request")!;
        _request.AddBaggage("probe", "carried");
        RequestContext.Set(TenantRequestContext.Key, "some-tenant");
        _context = await _fixture.GrainFactory.GetGrain<ITraceContextProbe>(Guid.NewGuid())
            .Read(true).WaitAsync(TimeSpan.FromSeconds(30));
    }

    [Fact] void should_carry_the_request_trace_across_both_grain_calls() => _context[0].ShouldEqual(_request.TraceId.ToString());
    [Fact] void should_carry_the_tenant_in_request_context() => _context[1].ShouldEqual("some-tenant");
    [Fact] void should_not_add_the_request_context_tenant_to_baggage() => _context[2].ShouldEqual(string.Empty);
    [Fact] void should_not_add_the_telemetry_tenant_to_baggage() => _context[3].ShouldEqual(string.Empty);
    [Fact] void should_propagate_explicit_non_tenant_baggage() => _context[4].ShouldEqual("carried");

    void Destroy()
    {
        RequestContext.Remove(TenantRequestContext.Key);
        _request?.Dispose();
        _source?.Dispose();
        _listener?.Dispose();
    }
}
