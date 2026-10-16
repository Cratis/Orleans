// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Shares direct job startup between the local API and the legacy manager.
/// </summary>
internal static class JobStarter
{
    /// <summary>
    /// Starts the addressed job and preserves its refusal.
    /// </summary>
    /// <param name="grainFactory">The caller's grain factory.</param>
    /// <param name="key">The storage coordinates.</param>
    /// <param name="jobId">The job identity.</param>
    /// <param name="request">The request.</param>
    /// <typeparam name="TJob">The job grain interface.</typeparam>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <returns>The started identity or refusal.</returns>
    internal static async Task<Result<JobId, StartJobError>> Start<TJob, TRequest>(IGrainFactory grainFactory, JobKey key, JobId jobId, TRequest request)
        where TJob : IJob<TRequest>
        where TRequest : class, IJobRequest
    {
        var job = grainFactory.GetGrain<TJob>(jobId, key);
        return (await job.Start(request)).Match<Result<JobId, StartJobError>>(_ => jobId, error => error);
    }
}
