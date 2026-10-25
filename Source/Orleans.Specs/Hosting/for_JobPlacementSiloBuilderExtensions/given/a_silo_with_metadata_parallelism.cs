// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Orleans.Hosting.for_JobPlacementSiloBuilderExtensions.given;

public class a_silo_with_metadata_parallelism : a_silo_builder
{
    protected IJobStepThrottle _throttle;

    void Establish()
    {
        _services.AddSingleton(Options.Create(Cratis.Orleans.Jobs.for_SiloMetadataJobStepParallelism.given.silo_metadata.MetadataWith((SiloMetadataJobStepParallelism.DefaultMetadataKey, "2"))));
        _services.AddSingleton(Options.Create(new JobsOptions { MaxParallelSteps = 5 }));
        _services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    protected static void AddTheDefaultThrottle(IServiceCollection services) => services.TryAddSingleton<IJobStepThrottle, JobStepThrottle>();

    protected static bool GrantsOnly(IJobStepThrottle throttle, int slots)
    {
        var granted = Enumerable.Range(0, slots).Select(_ => throttle.AcquireAsync()).ToArray();
        var next = throttle.AcquireAsync();
        return granted.All(task => task.IsCompletedSuccessfully) && !next.IsCompleted;
    }
}
