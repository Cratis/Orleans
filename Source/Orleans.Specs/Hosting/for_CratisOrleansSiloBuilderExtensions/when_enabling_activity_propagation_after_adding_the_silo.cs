// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Cratis.Orleans.Hosting.for_CratisOrleansSiloBuilderExtensions;

/// <summary>
/// Characterizes Orleans 10.3.1's non-idempotent registration; the hosting guide warns against this order.
/// </summary>
public class when_enabling_activity_propagation_after_adding_the_silo : Specification
{
    WebApplicationBuilder _builder;

    void Establish()
    {
        _builder = WebApplication.CreateBuilder();
        _builder.AddCratisOrleans();
    }

    void Because() => _builder.Host.UseOrleans(silo => silo.AddActivityPropagation());

    [Fact] void should_register_two_incoming_propagation_filters() => _builder.Services.Count(descriptor => descriptor.ServiceType == typeof(IIncomingGrainCallFilter) && descriptor.ImplementationType?.FullName == "Orleans.Runtime.ActivityPropagationIncomingGrainCallFilter").ShouldEqual(2);
    [Fact] void should_register_two_outgoing_propagation_filters() => _builder.Services.Count(descriptor => descriptor.ServiceType == typeof(IOutgoingGrainCallFilter) && descriptor.ImplementationType?.FullName == "Orleans.Runtime.ActivityPropagationOutgoingGrainCallFilter").ShouldEqual(2);
}
