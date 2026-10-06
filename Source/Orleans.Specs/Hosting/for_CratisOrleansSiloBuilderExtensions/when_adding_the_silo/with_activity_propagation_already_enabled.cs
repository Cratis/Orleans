// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Cratis.Orleans.Hosting.for_CratisOrleansSiloBuilderExtensions.when_adding_the_silo;

public class with_activity_propagation_already_enabled : Specification
{
    WebApplicationBuilder _builder;

    void Establish()
    {
        _builder = WebApplication.CreateBuilder();
        _builder.Host.UseOrleans(silo => silo.AddActivityPropagation());
    }

    void Because() => _builder.AddCratisOrleans();

    [Fact] void should_keep_one_incoming_propagation_filter() => _builder.Services.Count(descriptor => descriptor.ServiceType == typeof(IIncomingGrainCallFilter) && descriptor.ImplementationType?.FullName == "Orleans.Runtime.ActivityPropagationIncomingGrainCallFilter").ShouldEqual(1);
    [Fact] void should_keep_one_outgoing_propagation_filter() => _builder.Services.Count(descriptor => descriptor.ServiceType == typeof(IOutgoingGrainCallFilter) && descriptor.ImplementationType?.FullName == "Orleans.Runtime.ActivityPropagationOutgoingGrainCallFilter").ShouldEqual(1);
}
