// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Tenancy.for_TenantOutgoingCallFilter.when_a_call_leaves;

/// <summary>
/// A flow with no definite tenant records nothing - a deployment-wide fallback written here would
/// arrive at the receiving side looking like knowledge.
/// </summary>
public class and_the_flow_acts_for_no_tenant : Specification
{
    TenantOutgoingCallFilter _filter;
    IOutgoingGrainCallContext _context;
    string? _tenantTheCallCarried;
    bool _invoked;

    void Establish()
    {
        var tenancy = Substitute.For<ITenancy>();
        tenancy.Current.Returns((string?)null);
        _filter = new TenantOutgoingCallFilter(tenancy);
        _context = Substitute.For<IOutgoingGrainCallContext>();
        _context.When(c => c.Invoke()).Do(_ =>
        {
            _invoked = true;
            _tenantTheCallCarried = RequestContext.Get(TenantRequestContext.Key) as string;
        });
    }

    async Task Because() => await _filter.Invoke(_context);

    [Fact] public void should_carry_no_tenant() => _tenantTheCallCarried.ShouldBeNull();

    [Fact] public void should_still_invoke_the_call() => _invoked.ShouldBeTrue();
}
