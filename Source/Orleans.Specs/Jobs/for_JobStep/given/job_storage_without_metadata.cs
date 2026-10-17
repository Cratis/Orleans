// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Subjects;
using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;
using OneOf.Types;

using Catch = Cratis.Monads.Catch;

namespace Cratis.Orleans.Jobs.for_JobStep.given;

/// <summary>
/// A third-party-style provider with no GetJobType override, exercising the interface's compatibility fallback.
/// </summary>
/// <param name="storage">The full-job storage to forward to.</param>
public class job_storage_without_metadata(IJobStorage storage) : IJobStorage
{
    public Task<Catch<JobState, Storage.Jobs.JobError>> GetJob(JobId jobId) => storage.GetJob(jobId);
    public Task<Catch<IImmutableList<JobState>>> GetJobs(params JobStatus[] statuses) => storage.GetJobs(statuses);
    public Task<Catch<IImmutableList<JobState>>> GetJobs(JobQuery query) => storage.GetJobs(query);
    public Catch<ISubject<IEnumerable<JobState>>> ObserveJobs(params JobStatus[] statuses) => storage.ObserveJobs(statuses);
    public Task<Catch> Remove(JobId jobId) => storage.Remove(jobId);
    public Task<Catch<bool>> RemoveTerminal(JobId jobId) => storage.RemoveTerminal(jobId);
    public Task<Catch<TJobState, Storage.Jobs.JobError>> Read<TJobState>(JobId jobId) => storage.Read<TJobState>(jobId);
    public Task<Catch<None, Storage.Jobs.JobError>> Save<TJobState>(JobId jobId, TJobState state) => storage.Save(jobId, state);
    public Task<Catch<IImmutableList<TJobState>, Storage.Jobs.JobError>> GetJobs<TJobType, TJobState>(params JobStatus[] statuses) => storage.GetJobs<TJobType, TJobState>(statuses);
}
