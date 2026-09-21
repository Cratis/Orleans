// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Orleans.Tenancy.for_TenantIncomingCallFilter.when_a_call_arrives;

/// <summary>
/// A job step resumed long after the silo that started it is gone still carries its coordinates in
/// its own key, so the tenant survives restarts and resumes with it.
/// </summary>
public class and_the_target_is_a_job_step : given.an_incoming_call
{
    void Establish()
    {
        _tenancy.TenantFor("some-scope", "some-namespace").Returns("some-tenant");
        TargetIs<IJobStep>(GrainIdKeyExtensions.CreateGuidKey(
            Guid.NewGuid(),
            new JobStepKey(new(Guid.NewGuid()), "some-scope", "some-namespace")));
    }

    async Task Because() => await _filter.Invoke(_context);

    [Fact] public void should_establish_the_tenant_the_host_maps_the_key_to() => _establishedTenant.ShouldEqual("some-tenant");
}
