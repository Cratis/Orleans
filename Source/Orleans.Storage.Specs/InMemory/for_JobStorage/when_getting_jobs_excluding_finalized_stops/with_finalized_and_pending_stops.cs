// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Storage.InMemory.for_JobStorage.when_getting_jobs_excluding_finalized_stops;

public class with_finalized_and_pending_stops : given.jobs_in_storage
{
    readonly JobState _running = new() { Id = JobId.New(), Type = new("known-job"), Status = JobStatus.Running };
    readonly JobState _pendingStop = new() { Id = JobId.New(), Type = new("known-job"), Status = JobStatus.Stopped };
    readonly JobState _finalizedStop = new() { Id = JobId.New(), Type = new("known-job"), Status = JobStatus.Stopped, StopFinalized = true };
    readonly JobState _removing = new() { Id = JobId.New(), Type = new("known-job"), Status = JobStatus.Removing, StopFinalized = true };
    Catch<IImmutableList<JobState>> _result;

    async Task Establish()
    {
        foreach (var job in new[] { _running, _pendingStop, _finalizedStop, _removing })
        {
            (await _storage.Save(job.Id, job)).IsSuccess.ShouldBeTrue();
        }
    }

    async Task Because() => _result = await _storage.GetJobsExcludingFinalizedStops(JobStatus.Running, JobStatus.Stopped, JobStatus.Removing);

    [Fact] void should_leave_out_only_the_finalized_stop() => _result.AsT0.Select(job => job.Id).OrderBy(id => id.Value).ShouldEqual(new[] { _running.Id, _pendingStop.Id, _removing.Id }.OrderBy(id => id.Value));
}
