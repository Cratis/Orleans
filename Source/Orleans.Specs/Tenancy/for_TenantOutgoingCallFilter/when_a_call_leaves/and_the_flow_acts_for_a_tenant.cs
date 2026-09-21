// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Tenancy.for_TenantOutgoingCallFilter.when_a_call_leaves;

public class and_the_flow_acts_for_a_tenant : Specification
{
    TenantOutgoingCallFilter _filter;
    IOutgoingGrainCallContext _context;
    string? _tenantTheCallCarried;

    void Establish()
    {
        var tenancy = Substitute.For<ITenancy>();
        tenancy.Current.Returns("some-tenant");
        _filter = new TenantOutgoingCallFilter(tenancy);
        _context = Substitute.For<IOutgoingGrainCallContext>();
        _context.When(c => c.Invoke()).Do(_ => _tenantTheCallCarried = RequestContext.Get(TenantRequestContext.Key) as string);
    }

    async Task Because() => await _filter.Invoke(_context);

    void Destroy() => RequestContext.Remove(TenantRequestContext.Key);

    [Fact] public void should_carry_the_tenant_onto_the_call() => _tenantTheCallCarried.ShouldEqual("some-tenant");
}
