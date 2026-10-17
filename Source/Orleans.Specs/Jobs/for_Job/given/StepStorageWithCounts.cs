// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;

using Catch = Cratis.Monads.Catch;

namespace Cratis.Orleans.Jobs.for_Job.given;

/// <summary>
/// A step storage that answers what a specification arranges for counting steps by status and hands everything else
/// to the substitute it wraps - a substitute cannot be configured for a default interface member.
/// </summary>
/// <param name="inner">The substitute that answers everything else.</param>
public class StepStorageWithCounts(IJobStepStorage inner) : IJobStepStorage
{
    /// <summary>
    /// Gets or sets what counting the steps by status answers; null answers that storage failed.
    /// </summary>
    public IReadOnlyDictionary<JobStepStatus, int>? Counts { get; set; }

    /// <summary>
    /// Gets how many times the steps were counted by status.
    /// </summary>
    public int CountCalls { get; private set; }

    /// <inheritdoc/>
    public Task<Catch<IReadOnlyDictionary<JobStepStatus, int>>> CountByStatus(JobId jobId)
    {
        CountCalls++;
        if (Counts is null)
        {
            return Task.FromResult<Catch<IReadOnlyDictionary<JobStepStatus, int>>>(new InvalidOperationException("storage is down"));
        }
        return Task.FromResult(Catch<IReadOnlyDictionary<JobStepStatus, int>>.Success(Counts));
    }

    /// <inheritdoc/>
    public Task<Catch> RemoveAllForJob(JobId jobId) => inner.RemoveAllForJob(jobId);

    /// <inheritdoc/>
    public Task<Catch> RemoveAllNonFailedForJob(JobId jobId) => inner.RemoveAllNonFailedForJob(jobId);

    /// <inheritdoc/>
    public Task<Catch> Remove(JobId jobId, JobStepId jobStepId) => inner.Remove(jobId, jobStepId);

    /// <inheritdoc/>
    public Task<Catch<IImmutableList<JobStepState>>> GetForJob(JobId jobId, params JobStepStatus[] statuses) => inner.GetForJob(jobId, statuses);

    /// <inheritdoc/>
    public Task<Catch<int>> CountForJob(JobId jobId, params JobStepStatus[] statuses) => inner.CountForJob(jobId, statuses);

    /// <inheritdoc/>
    public Catch<IObservable<IEnumerable<JobStepState>>> ObserveForJob(JobId jobId) => inner.ObserveForJob(jobId);

    /// <inheritdoc/>
    public Task<Catch<TJobStepState, Storage.Jobs.JobStepError>> Read<TJobStepState>(JobId jobId, JobStepId jobStepId) => inner.Read<TJobStepState>(jobId, jobStepId);

    /// <inheritdoc/>
    public Task<Catch<OneOf.Types.None, Storage.Jobs.JobStepError>> Save<TJobStepState>(JobId jobId, JobStepId jobStepId, TJobStepState state) => inner.Save(jobId, jobStepId, state);

    /// <inheritdoc/>
    public Task<Catch<OneOf.Types.None, Storage.Jobs.JobStepError>> MoveToFailed<TJobStepState>(JobId jobId, JobStepId jobStepId, TJobStepState jobStepState) => inner.MoveToFailed(jobId, jobStepId, jobStepState);
}
