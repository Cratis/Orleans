// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive.Subjects;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using OneOf.Types;

namespace Cratis.Orleans.Storage.InMemory;

/// <summary>
/// Represents an in-memory implementation of <see cref="IJobStorage"/>.
/// </summary>
/// <param name="jobTypes"><see cref="IJobTypes"/> that knows about <see cref="JobType"/>.</param>
public sealed class JobStorage(IJobTypes jobTypes) : IJobStorage, IDisposable
{
    readonly ConcurrentDictionary<JobId, JobState> _jobs = new();
    readonly ReplaySubject<IEnumerable<JobState>> _subject = new(1);

    /// <inheritdoc/>
    public Task<Catch<JobState, JobError>> GetJob(JobId jobId)
    {
        try
        {
            return Task.FromResult<Catch<JobState, JobError>>(
                _jobs.TryGetValue(jobId, out var job) ? job : JobError.NotFound);
        }
        catch (Exception ex)
        {
            return Task.FromResult<Catch<JobState, JobError>>(ex);
        }
    }

    /// <inheritdoc/>
    public Task<Catch<IImmutableList<JobState>>> GetJobs(params JobStatus[] statuses)
    {
        try
        {
            var jobs = FilterByStatus(_jobs.Values, statuses).ToImmutableList();
            return Task.FromResult<Catch<IImmutableList<JobState>>>(jobs);
        }
        catch (Exception ex)
        {
            return Task.FromResult<Catch<IImmutableList<JobState>>>(ex);
        }
    }

    /// <inheritdoc/>
    public Task<Catch<IImmutableList<JobState>>> GetJobs(JobQuery query)
    {
        try
        {
            var jobs = _jobs.Values.Where(job =>
                (query.Type is null || job.Type == query.Type) &&
                (query.Statuses.Count == 0 || query.Statuses.Contains(job.Status)) &&
                (query.CreatedBefore is null || job.Created < query.CreatedBefore))
                .OrderBy(job => job.Created)
                .ThenBy(job => job.Id)
                .Skip(Math.Max(0, query.Skip));
            var page = query.Take > 0 ? jobs.Take(query.Take) : jobs;
            return Task.FromResult<Catch<IImmutableList<JobState>>>(page.ToImmutableList());
        }
        catch (Exception ex)
        {
            return Task.FromResult<Catch<IImmutableList<JobState>>>(ex);
        }
    }

    /// <inheritdoc/>
    public Catch<ISubject<IEnumerable<JobState>>> ObserveJobs(params JobStatus[] statuses)
    {
        try
        {
            PublishSnapshot();
            return Catch.Success<ISubject<IEnumerable<JobState>>>(_subject);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public Task<Catch> Remove(JobId jobId)
    {
        try
        {
            _jobs.TryRemove(jobId, out _);
            PublishSnapshot();
            return Task.FromResult(Catch.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult<Catch>(ex);
        }
    }

    /// <inheritdoc/>
    public Task<Catch<bool>> RemoveTerminal(JobId jobId)
    {
        try
        {
            if (!_jobs.TryGetValue(jobId, out var job) || !IsTerminal(job.Status))
            {
                return Task.FromResult<Catch<bool>>(false);
            }
            var removed = ((ICollection<KeyValuePair<JobId, JobState>>)_jobs).Remove(new(jobId, job));
            if (removed)
            {
                PublishSnapshot();
            }
            return Task.FromResult<Catch<bool>>(removed);
        }
        catch (Exception ex)
        {
            return Task.FromResult<Catch<bool>>(ex);
        }
    }

    /// <inheritdoc/>
    public Task<Catch<TJobState, JobError>> Read<TJobState>(JobId jobId)
    {
        try
        {
            if (JobStateType.Verify(typeof(TJobState)).TryGetError(out var error))
            {
                return Task.FromResult<Catch<TJobState, JobError>>(error);
            }

            if (_jobs.TryGetValue(jobId, out var job) && job is TJobState typed)
            {
                return Task.FromResult<Catch<TJobState, JobError>>(typed);
            }

            return Task.FromResult<Catch<TJobState, JobError>>(JobError.NotFound);
        }
        catch (Exception ex)
        {
            return Task.FromResult<Catch<TJobState, JobError>>(ex);
        }
    }

    /// <inheritdoc/>
    public Task<Catch<None, JobError>> Save<TJobState>(JobId jobId, TJobState state)
    {
        try
        {
            if (JobStateType.Verify(typeof(TJobState)).TryGetError(out var error))
            {
                return Task.FromResult<Catch<None, JobError>>(error);
            }

            _jobs[jobId] = (state as JobState)!;
            PublishSnapshot();
            return Task.FromResult<Catch<None, JobError>>(default(None));
        }
        catch (Exception ex)
        {
            return Task.FromResult<Catch<None, JobError>>(ex);
        }
    }

    /// <inheritdoc/>
    public Task<Catch<IImmutableList<TJobState>, JobError>> GetJobs<TJobType, TJobState>(params JobStatus[] statuses)
    {
        try
        {
            if (JobStateType.Verify(typeof(TJobState)).TryGetError(out var error))
            {
                return Task.FromResult<Catch<IImmutableList<TJobState>, JobError>>(error);
            }

            if (jobTypes.GetFor(typeof(TJobType)).TryPickT1(out _, out var jobType))
            {
                return Task.FromResult<Catch<IImmutableList<TJobState>, JobError>>(JobError.TypeIsNotAssociatedWithAJobType);
            }

            var jobs = FilterByStatus(_jobs.Values.Where(_ => _.Type == jobType), statuses)
                .OfType<TJobState>()
                .ToImmutableList();

            return Task.FromResult<Catch<IImmutableList<TJobState>, JobError>>(jobs);
        }
        catch (Exception ex)
        {
            return Task.FromResult<Catch<IImmutableList<TJobState>, JobError>>(ex);
        }
    }

    /// <inheritdoc/>
    public void Dispose() => _subject.Dispose();

    static IEnumerable<JobState> FilterByStatus(IEnumerable<JobState> jobs, JobStatus[] statuses) =>
        statuses.Length == 0 ? jobs : jobs.Where(_ => statuses.Contains(_.Status));

    static bool IsTerminal(JobStatus status) => status is JobStatus.CompletedSuccessfully or JobStatus.CompletedWithFailures or JobStatus.Failed;

    void PublishSnapshot() => _subject.OnNext([.. _jobs.Values]);
}
