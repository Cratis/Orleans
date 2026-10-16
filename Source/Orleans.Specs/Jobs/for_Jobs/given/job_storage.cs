// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Subjects;
using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;
using OneOf.Types;

using Catch = Cratis.Monads.Catch;

namespace Cratis.Orleans.Jobs.for_Jobs.given;

/// <summary>
/// Castle cannot intercept the new default interface method on .NET 10. This forwarding adapter supplies
/// virtual methods for the unit doubles without changing the production compatibility fallback.
/// </summary>
/// <param name="storage">The underlying storage to forward to.</param>
public class job_storage(IJobStorage storage) : IJobStorage
{
    public virtual Task<Catch<JobType, Storage.Jobs.JobError>> GetJobType(JobId jobId) => storage.GetJobType(jobId);
    public virtual Task<Catch<JobState, Storage.Jobs.JobError>> GetJob(JobId jobId) => storage.GetJob(jobId);
    public virtual Task<Catch<IImmutableList<JobState>>> GetJobs(params JobStatus[] statuses) => storage.GetJobs(statuses);
    public virtual Task<Catch<IImmutableList<JobState>>> GetJobs(JobQuery query) => storage.GetJobs(query);
    public virtual Catch<ISubject<IEnumerable<JobState>>> ObserveJobs(params JobStatus[] statuses) => storage.ObserveJobs(statuses);
    public virtual Task<Catch> Remove(JobId jobId) => storage.Remove(jobId);
    public virtual Task<Catch<bool>> RemoveTerminal(JobId jobId) => storage.RemoveTerminal(jobId);
    public virtual Task<Catch<TJobState, Storage.Jobs.JobError>> Read<TJobState>(JobId jobId) => storage.Read<TJobState>(jobId);
    public virtual Task<Catch<None, Storage.Jobs.JobError>> Save<TJobState>(JobId jobId, TJobState state) => storage.Save(jobId, state);
    public virtual Task<Catch<IImmutableList<TJobState>, Storage.Jobs.JobError>> GetJobs<TJobType, TJobState>(params JobStatus[] statuses) => storage.GetJobs<TJobType, TJobState>(statuses);
}
