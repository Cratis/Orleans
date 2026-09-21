// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Orleans.Tenancy.for_TenancySiloBuilderExtensions;

public class when_adding_tenancy : Specification
{
    ISiloBuilder _silo;
    ServiceCollection _services;

    void Establish()
    {
        _services = [];
        _silo = Substitute.For<ISiloBuilder>();
        _silo.Services.Returns(_services);
    }

    void Because() => _silo.AddTenancy<SomeTenancy>();

    [Fact] public void should_register_the_hosts_tenancy() => _services.ShouldContain(descriptor => descriptor.ServiceType == typeof(ITenancy) && descriptor.ImplementationType == typeof(SomeTenancy));

    [Fact] public void should_register_the_incoming_filter() => _services.ShouldContain(descriptor => descriptor.ServiceType == typeof(IIncomingGrainCallFilter) && descriptor.ImplementationType == typeof(TenantIncomingCallFilter));

    [Fact] public void should_register_the_outgoing_filter() => _services.ShouldContain(descriptor => descriptor.ServiceType == typeof(IOutgoingGrainCallFilter) && descriptor.ImplementationType == typeof(TenantOutgoingCallFilter));

    class SomeTenancy : ITenancy
    {
        public string? Current => null;

        public string? TenantFor(string scope, string @namespace) => null;

        public IDisposable Establish(string tenant) => throw new NotImplementedException();
    }
}
