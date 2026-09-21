// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Tenancy.for_TenantIncomingCallFilter.when_a_call_arrives;

public class and_nothing_names_a_tenant : given.an_incoming_call
{
    void Establish() => _target.GrainInstance.Returns(new object());

    async Task Because() => await _filter.Invoke(_context);

    [Fact] public void should_establish_nothing() => _establishedTenant.ShouldBeNull();

    [Fact] public void should_still_invoke_the_call() => _invoked.ShouldBeTrue();
}
