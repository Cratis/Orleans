// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Orleans.Metadata;
using Orleans.Runtime.Placement;

namespace Cratis.Orleans.Hosting.for_JobPlacementSiloBuilderExtensions.given;

public class a_silo_builder : Specification
{
    protected ISiloBuilder _silo;
    protected ServiceCollection _services;

    void Establish()
    {
        _services = [];
        _silo = Substitute.For<ISiloBuilder>();
        _silo.Services.Returns(_services);
    }

    protected int Strategies(string name) => _services.Count(descriptor => descriptor.ServiceType == typeof(PlacementStrategy) && Equals(descriptor.ServiceKey, name));

    protected IEnumerable<ServiceDescriptor> Directors(Type strategy) =>
        _services.Where(descriptor => descriptor.ServiceType == typeof(IPlacementDirector) && Equals(descriptor.ServiceKey, strategy));

    protected Type DirectorOf(Type strategy) => Directors(strategy).Last().KeyedImplementationType!;

    protected int Providers<TProvider>() =>
        _services.Count(descriptor => descriptor.ServiceType == typeof(IGrainPropertiesProvider) && descriptor.ImplementationType == typeof(TProvider));

    protected class HostDirector : IPlacementDirector
    {
        public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context) =>
            Task.FromResult(context.LocalSilo);
    }
}
