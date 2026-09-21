// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Orleans.Tenancy.for_TenantIncomingCallFilter.when_a_call_arrives;

public class and_the_host_maps_the_coordinates_to_no_tenant : given.an_incoming_call
{
    void Establish()
    {
        _tenancy.TenantFor("some-scope", "some-namespace").Returns((string?)null);
        TargetIs<IJob>(GrainIdKeyExtensions.CreateGuidKey(Guid.NewGuid(), new JobKey("some-scope", "some-namespace")));
    }

    async Task Because() => await _filter.Invoke(_context);

    [Fact] public void should_establish_nothing() => _establishedTenant.ShouldBeNull();

    [Fact] public void should_still_invoke_the_call() => _invoked.ShouldBeTrue();
}
