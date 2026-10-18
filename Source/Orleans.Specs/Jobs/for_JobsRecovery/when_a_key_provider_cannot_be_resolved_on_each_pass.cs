// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Orleans.Jobs.for_JobsRecovery;

public class when_a_key_provider_cannot_be_resolved_on_each_pass : given.a_recovery_participant
{
    int _failedResolutions;
    ServiceProvider _services;

    protected override IInstancesOf<IJobsManagerKeys> CreateProviders(IJobsManagerKeys keys)
    {
        var services = new ServiceCollection();
        var types = Substitute.For<ITypes>();
        types.FindMultiple<IJobsManagerKeys>().Returns([typeof(IJobsManagerKeys), keys.GetType()]);
        services.AddSingleton(types);
        services.AddSingleton(keys.GetType(), keys);
        services.AddSingleton<IJobsManagerKeys>(_ =>
        {
            _failedResolutions++;
            throw new Exception("provider construction failed");
        });
        services.AddSingleton<ILogger<JobsRecovery>>(NullLogger<JobsRecovery>.Instance);
        var silo = Substitute.For<ISiloBuilder>();
        silo.Services.Returns(services);
        silo.AddCratisOrleansJobsRecovery();
        _services = services.BuildServiceProvider();
        return _services.GetRequiredService<IInstancesOf<IJobsManagerKeys>>();
    }

    async Task Because()
    {
        await Start();
        await Publish(SiloStatus.Dead);
    }

    [Fact] void should_retry_resolution_on_each_pass() => _failedResolutions.ShouldEqual(4);
    [Fact] void should_recover_the_other_providers() => _first.Received(1).Rehydrate();
    [Fact] void should_keep_watching_membership() => _second.Received(1).Rehydrate();

    void Destroy() => _services.Dispose();
}
