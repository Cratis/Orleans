// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Starts, stops and deletes jobs without sending job types through a shared manager grain.
/// </summary>
public interface IJobs
{
    /// <summary>
    /// Starts a job with a new identity.
    /// </summary>
    /// <param name="key">The job's storage scope and namespace.</param>
    /// <param name="request">The request to pass to the job.</param>
    /// <typeparam name="TJob">The job grain interface.</typeparam>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <returns>The started identity or the job's refusal.</returns>
    Task<Result<JobId, StartJobError>> Start<TJob, TRequest>(JobKey key, TRequest request)
        where TJob : IJob<TRequest>
        where TRequest : class, IJobRequest;

    /// <summary>
    /// Starts a job with a caller-supplied identity.
    /// </summary>
    /// <param name="key">The job's storage scope and namespace.</param>
    /// <param name="jobId">The job identity.</param>
    /// <param name="request">The request to pass to the job.</param>
    /// <typeparam name="TJob">The job grain interface.</typeparam>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <returns>The started identity or the job's refusal, including duplicate starts.</returns>
    Task<Result<JobId, StartJobError>> Start<TJob, TRequest>(JobKey key, JobId jobId, TRequest request)
        where TJob : IJob<TRequest>
        where TRequest : class, IJobRequest;

    /// <summary>
    /// Stops a known job through its grain, without deserializing its stored request in the caller.
    /// </summary>
    /// <param name="key">The job's storage scope and namespace.</param>
    /// <param name="jobId">The job identity.</param>
    /// <returns>The stop outcome. Storage and grain-call exceptions propagate.</returns>
    Task<StopJobResult> Stop(JobKey key, JobId jobId);

    /// <summary>
    /// Removes a known job through its grain, with the same removal rules as the jobs manager.
    /// </summary>
    /// <param name="key">The job's storage scope and namespace.</param>
    /// <param name="jobId">The job identity.</param>
    /// <returns>The removal outcome. Storage and grain-call exceptions propagate.</returns>
    Task<DeleteJobResult> Delete(JobKey key, JobId jobId);

    /// <summary>
    /// Stops a known job and waits until its steps are drained, through its grain; see <see cref="IJobDrain.StopAndDrain"/>.
    /// </summary>
    /// <param name="key">The job's storage scope and namespace.</param>
    /// <param name="jobId">The job identity.</param>
    /// <param name="timeout">How long to wait. <see cref="TimeSpan.Zero"/> polls without waiting.</param>
    /// <returns>The drain outcome. Storage and grain-call exceptions propagate.</returns>
    /// <remarks>
    /// Use only once every silo runs a release with <see cref="IJobDrain"/>. Calling again continues a drain that
    /// timed out.
    /// </remarks>
    Task<DrainJobResult> StopAndDrain(JobKey key, JobId jobId, TimeSpan timeout);
}
