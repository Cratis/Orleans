// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Tenancy.for_TenantIncomingCallFilter.when_a_call_arrives;

/// <summary>
/// A grain whose key names no tenant still learns which tenant asked, from the entry the outgoing
/// filter wrote onto the call. The entry is set inside the same flow that invokes - RequestContext
/// is AsyncLocal, so a value set in Establish would not reach the invocation's execution context.
/// </summary>
public class and_the_caller_carried_a_tenant_in_the_request_context : given.an_incoming_call
{
    void Establish() => _target.GrainInstance.Returns(new object());

    async Task Because()
    {
        RequestContext.Set(TenantRequestContext.Key, "some-tenant");
        await _filter.Invoke(_context);
    }

    [Fact] public void should_establish_the_tenant_the_caller_sent() => _establishedTenant.ShouldEqual("some-tenant");
}
