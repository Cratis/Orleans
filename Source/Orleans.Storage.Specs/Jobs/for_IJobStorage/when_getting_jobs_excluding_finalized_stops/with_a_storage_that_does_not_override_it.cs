// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Orleans.Storage.Jobs.for_IJobStorage.when_getting_jobs_excluding_finalized_stops;

public class with_a_storage_that_does_not_override_it : given.a_legacy_store
{
    static readonly JobStatus[] _statuses = [JobStatus.Running, JobStatus.Stopped, JobStatus.Removing];
    readonly JobState _running = new() { Id = JobId.New(), Status = JobStatus.Running };
    readonly JobState _pendingStop = new() { Id = JobId.New(), Status = JobStatus.Stopped };
    readonly JobState _finalizedStop = new() { Id = JobId.New(), Status = JobStatus.Stopped, StopFinalized = true };
    readonly JobState _removing = new() { Id = JobId.New(), Status = JobStatus.Removing, StopFinalized = true };
    Catch<IImmutableList<JobState>> _result;

    void Establish() => _original.GetJobs(Arg.Any<JobStatus[]>()).Returns(
        Task.FromResult<Catch<IImmutableList<JobState>>>(ImmutableList.Create(_running, _pendingStop, _finalizedStop, _removing)));

    async Task Because() => _result = await _legacy.GetJobsExcludingFinalizedStops(_statuses);

    [Fact] void should_leave_out_only_the_finalized_stop() => _result.AsT0.ShouldEqual([_running, _pendingStop, _removing]);
    [Fact] void should_read_the_requested_statuses() => _original.Received(1).GetJobs(Arg.Is<JobStatus[]>(s => s.SequenceEqual(_statuses)));
}
