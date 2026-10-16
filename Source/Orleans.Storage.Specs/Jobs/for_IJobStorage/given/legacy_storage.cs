// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Subjects;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using OneOf.Types;

namespace Cratis.Orleans.Storage.Jobs.for_IJobStorage.given;

public class legacy_storage(IJobStorage storage) : IJobStorage
{
    public Task<Catch<JobState, JobError>> GetJob(JobId jobId) => storage.GetJob(jobId);
    public Task<Catch<IImmutableList<JobState>>> GetJobs(params JobStatus[] statuses) => storage.GetJobs(statuses);
    public Task<Catch<IImmutableList<JobState>>> GetJobs(JobQuery query) => storage.GetJobs(query);
    public Catch<ISubject<IEnumerable<JobState>>> ObserveJobs(params JobStatus[] statuses) => storage.ObserveJobs(statuses);
    public Task<Cratis.Monads.Catch> Remove(JobId jobId) => storage.Remove(jobId);
    public Task<Catch<bool>> RemoveTerminal(JobId jobId) => storage.RemoveTerminal(jobId);
    public Task<Catch<TJobState, JobError>> Read<TJobState>(JobId jobId) => storage.Read<TJobState>(jobId);
    public Task<Catch<None, JobError>> Save<TJobState>(JobId jobId, TJobState state) => storage.Save(jobId, state);
    public Task<Catch<IImmutableList<TJobState>, JobError>> GetJobs<TJobType, TJobState>(params JobStatus[] statuses) => storage.GetJobs<TJobType, TJobState>(statuses);
}
