// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Subjects;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using OneOf.Types;

namespace Cratis.Orleans.Storage.Jobs;

/// <summary>
/// Defines a system that can store job state.
/// </summary>
public interface IJobStorage
{
    /// <summary>
    /// Get a specific job.
    /// </summary>
    /// <param name="jobId">The unique <see cref="JobId"/> for the job to get.</param>
    /// <returns>The job instance.</returns>
    Task<Catch<JobState, JobError>> GetJob(JobId jobId);

    /// <summary>
    /// Gets the stored type without reading the job's request.
    /// </summary>
    /// <param name="jobId">The job identity.</param>
    /// <returns>The stored type, not found, or a storage exception.</returns>
    /// <remarks>
    /// Providers should override this with a metadata-only read. The compatibility fallback reads the
    /// full job, so it cannot identify an unknown type whose request cannot be deserialized.
    /// </remarks>
    async Task<Catch<JobType, JobError>> GetJobType(JobId jobId) => (await GetJob(jobId)).Match<Catch<JobType, JobError>>(
        job => job.Type,
        error => error,
        exception => exception);

    /// <summary>
    /// Get all jobs of a given type with a given status.
    /// </summary>
    /// <param name="statuses">Optional params of <see cref="JobStatus"/> to filter on.</param>
    /// <returns>A collection of job state objects.</returns>
    /// <remarks>
    /// If no job statuses are specified, all jobs will be returned.
    /// </remarks>
    Task<Catch<IImmutableList<JobState>>> GetJobs(params JobStatus[] statuses);

    /// <summary>
    /// Gets one bounded page of jobs selected by storage-side filters.
    /// </summary>
    /// <param name="query">The type, status, age, and page selection.</param>
    /// <returns>A page of job state objects, ordered by creation time and identity.</returns>
    Task<Catch<IImmutableList<JobState>>> GetJobs(JobQuery query);

    /// <summary>
    /// Observe jobs of a given type with a given status.
    /// </summary>
    /// <param name="statuses">Optional params of <see cref="JobStatus"/> to filter on.</param>
    /// <returns>An observable of collection of job state objects.</returns>
    /// <remarks>
    /// If no job statuses are specified, all jobs will be observed.
    /// </remarks>
    Catch<ISubject<IEnumerable<JobState>>> ObserveJobs(params JobStatus[] statuses);

    /// <summary>
    /// Remove a job.
    /// </summary>
    /// <param name="jobId">The <see cref="JobId"/> of the job to remove.</param>
    /// <returns>Awaitable task.</returns>
    Task<Catch> Remove(JobId jobId);

    /// <summary>
    /// Removes a job only when it is in an immutable terminal state.
    /// </summary>
    /// <param name="jobId">The <see cref="JobId"/> of the job to remove.</param>
    /// <returns>Whether a terminal job was removed.</returns>
    /// <remarks>
    /// This conditional operation is intentionally separate from normal job removal so retention can never
    /// delete a preparing, running, stopped, or removing job.
    /// </remarks>
    Task<Catch<bool>> RemoveTerminal(JobId jobId);

    /// <summary>
    /// Read the state of a job.
    /// </summary>
    /// <param name="jobId">The <see cref="JobId"/> of the job to read.</param>
    /// <returns>Job state instance or null if it doesn't exist.</returns>
    /// <typeparam name="TJobState">Type of <see cref="JobState"/> to return.</typeparam>
    Task<Catch<TJobState, JobError>> Read<TJobState>(JobId jobId);

    /// <summary>
    /// Save the state of a job.
    /// </summary>
    /// <param name="jobId">The <see cref="JobId"/> of the job to save.</param>
    /// <param name="state"><see cref="JobState"/> to save.</param>
    /// <typeparam name="TJobState">Type of <see cref="JobState"/> to save.</typeparam>
    /// <returns>Awaitable task.</returns>
    Task<Catch<None, JobError>> Save<TJobState>(JobId jobId, TJobState state);

    /// <summary>
    /// Get all jobs of a given type with a given status.
    /// </summary>
    /// <typeparam name="TJobType">Type of job to get for.</typeparam>
    /// <param name="statuses">Optional params of <see cref="JobStatus"/> to filter on.</param>
    /// <returns>A collection of job state objects.</returns>
    /// <typeparam name="TJobState">Type of <see cref="JobState"/> to return.</typeparam>
    /// <remarks>
    /// If no job statuses are specified, all jobs of the given type will be returned.
    /// </remarks>
    Task<Catch<IImmutableList<TJobState>, JobError>> GetJobs<TJobType, TJobState>(params JobStatus[] statuses);
}
