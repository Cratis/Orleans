// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;

namespace Cratis.Orleans.Jobs.Integration.for_JobsManager.given;

public class a_jobs_manager_with_lifecycle : Specification
{
    protected TestCluster _cluster;
    protected RehydrationLifecycle _lifecycle;
    protected IRehydratingJobsManager _manager;

    protected static TimeSpan Deadline => TimeSpan.FromSeconds(10);

    async Task Establish()
    {
        _cluster = new TestClusterBuilder(1)
            .AddSiloBuilderConfigurator<RehydrationClusterConfigurator>()
            .AddClientBuilderConfigurator<RehydrationClusterConfigurator>()
            .Build();
        await _cluster.DeployAsync();
        var services = ((InProcessSiloHandle)_cluster.Primary).SiloHost.Services;
        _lifecycle = services.GetRequiredService<RehydrationLifecycle>();
        _manager = _cluster.GrainFactory.GetGrain<IRehydratingJobsManager>(0, new JobsManagerKey("rehydration-lifecycle", string.Empty));
    }

    async Task Destroy()
    {
        _lifecycle.AllowDiscovery.TrySetResult();
        _lifecycle.AllowFirstRead.TrySetResult();
        _lifecycle.FinishRequest.TrySetResult();
        await _cluster.StopAllSilosAsync();
        await _cluster.DisposeAsync();
    }
}
