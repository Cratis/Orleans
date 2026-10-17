// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Orleans.Hosting.for_CratisOrleansSiloBuilderExtensions;

public class when_adding_job_admission_twice : Specification
{
    ISiloBuilder _silo;
    ServiceCollection _services;

    void Establish()
    {
        _services = [];
        _silo = Substitute.For<ISiloBuilder>();
        _silo.Services.Returns(_services);
    }

    void Because()
    {
        _silo.AddJobAdmission<SomeAdmission>();
        _silo.AddJobAdmission<SomeAdmission>();
    }

    [Fact] void should_register_it_once() => _services.Count(descriptor => descriptor.ServiceType == typeof(IJobAdmission) && descriptor.ImplementationType == typeof(SomeAdmission)).ShouldEqual(1);
    [Fact] void should_register_it_as_a_singleton() => _services.Single(descriptor => descriptor.ServiceType == typeof(IJobAdmission)).Lifetime.ShouldEqual(ServiceLifetime.Singleton);

    class SomeAdmission : IJobAdmission
    {
        public Task<bool> MayRun(JobAdmissionRequest request, CancellationToken cancellationToken) => Task.FromResult(true);
    }
}
