// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Orleans.Jobs.for_JobsRecovery.given;

public class a_recovery_participant_with_unresolved_provider : a_recovery_participant
{
    protected int _resolutions;
    protected IJobsManagerKeys _unresolved;
    ServiceProvider _services;

    protected virtual int FailedResolutions => 2;

    protected override IInstancesOf<IJobsManagerKeys> CreateProviders(IJobsManagerKeys keys)
    {
        _unresolved = Substitute.For<IJobsManagerKeys>();
        var services = new ServiceCollection();
        var types = Substitute.For<ITypes>();
        types.FindMultiple<IJobsManagerKeys>().Returns([typeof(IJobsManagerKeys), keys.GetType()]);
        services.AddSingleton(types);
        services.AddSingleton(keys.GetType(), keys);
        services.AddSingleton<IJobsManagerKeys>(_ =>
        {
            if (++_resolutions <= FailedResolutions)
            {
                throw new Exception("provider construction failed");
            }
            return _unresolved;
        });
        services.AddSingleton<ILogger<JobsRecovery>>(NullLogger<JobsRecovery>.Instance);
        var silo = Substitute.For<ISiloBuilder>();
        silo.Services.Returns(services);
        silo.AddCratisOrleansJobsRecovery();
        _services = services.BuildServiceProvider();
        return _services.GetRequiredService<IInstancesOf<IJobsManagerKeys>>();
    }

    void Establish()
    {
        _keys.GetKeys(Arg.Any<CancellationToken>()).Returns(
            Task.FromResult<IEnumerable<JobsManagerKey>>([new("scope", "second")]));
        _unresolved.GetKeys(Arg.Any<CancellationToken>()).Returns(
            Task.FromResult<IEnumerable<JobsManagerKey>>([new("scope", "first")]));
    }

    void Destroy() => _services.Dispose();
}
