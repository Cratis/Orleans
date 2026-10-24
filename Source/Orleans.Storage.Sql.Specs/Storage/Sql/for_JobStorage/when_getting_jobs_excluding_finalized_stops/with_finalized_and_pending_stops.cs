// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Storage.Sql.for_JobStorage.when_getting_jobs_excluding_finalized_stops;

public class with_finalized_and_pending_stops : given.jobs_in_storage
{
    JobState _running;
    JobState _pendingStop;
    JobState _finalizedStop;
    JobState _removing;
    Catch<IImmutableList<JobState>> _result;

    Task Establish()
    {
        _running = new JobState { Id = JobId.New(), Type = KnownJobType, Status = JobStatus.Running, Request = new KnownRequest("running") };
        _pendingStop = new JobState { Id = JobId.New(), Type = KnownJobType, Status = JobStatus.Stopped, Request = new KnownRequest("pending") };
        _finalizedStop = new JobState { Id = JobId.New(), Type = KnownJobType, Status = JobStatus.Stopped, StopFinalized = true, Request = new KnownRequest("finalized") };
        _removing = new JobState { Id = JobId.New(), Type = KnownJobType, Status = JobStatus.Removing, StopFinalized = true, Request = new KnownRequest("removing") };
        return WithJobs(ToEntity(_running), ToEntity(_pendingStop), ToEntity(_finalizedStop), ToEntity(_removing), _unknownJob);
    }

    async Task Because() => _result = await _storage.GetJobsExcludingFinalizedStops(JobStatus.Running, JobStatus.Stopped, JobStatus.Removing);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_leave_out_only_the_finalized_stop() => _result.AsT0.Select(job => job.Id).ShouldContainOnly(_running.Id, _pendingStop.Id, _removing.Id);
    [Fact] void should_deserialize_the_requests() => _result.AsT0.Select(job => job.Request).ShouldContainOnly(_running.Request, _pendingStop.Request, _removing.Request);
}
