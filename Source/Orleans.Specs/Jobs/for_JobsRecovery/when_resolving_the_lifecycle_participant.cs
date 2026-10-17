// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Orleans.Jobs.for_JobsRecovery;

public class when_resolving_the_lifecycle_participant : Specification
{
    ServiceProvider _services;
    ILifecycleParticipant<ISiloLifecycle> _participant;

    void Establish()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IClusterMembershipService>());
        services.AddSingleton(Substitute.For<ILocalSiloDetails>());
        services.AddSingleton<IInstancesOf<IJobsManagerKeys>>(new KnownInstancesOf<IJobsManagerKeys>());
        services.AddSingleton(Substitute.For<IGrainFactory>());
        services.AddSingleton<ILogger<JobsRecovery>>(NullLogger<JobsRecovery>.Instance);
        services.Add(ServiceDescriptor.Singleton<ILifecycleParticipant<ISiloLifecycle>, JobsRecovery>());
        _services = services.BuildServiceProvider();
    }

    void Because() => _participant = _services.GetRequiredService<ILifecycleParticipant<ISiloLifecycle>>();

    [Fact] void should_resolve_using_the_existing_public_constructor() => _participant.ShouldBeOfExactType<JobsRecovery>();

    void Destroy() => _services.Dispose();
}
