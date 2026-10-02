// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Storage.Sql.for_JobStorage.when_observing_jobs;

public class with_an_unknown_type : given.jobs_in_storage
{
    JobState[] _result;

    Task Establish() => WithJobs(_unknownJob, ToEntity(_firstJob), ToEntity(_secondJob));

    async Task Because()
    {
        var completion = new TaskCompletionSource<JobState[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = _storage.ObserveJobs(JobStatus.Running).AsT0.Where(jobs => jobs.Any()).Subscribe(
            jobs => completion.TrySetResult(jobs.ToArray()),
            error => completion.TrySetException(error));
        _result = await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_emit_both_registered_jobs() => _result.Select(job => job.Id).ShouldContainOnly(_firstJob.Id, _secondJob.Id);
    [Fact] void should_deserialize_the_known_requests() => _result.Select(job => job.Request).ShouldContainOnly(_firstJob.Request, _secondJob.Request);
}
