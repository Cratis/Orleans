// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStorage.when_getting_jobs_excluding_finalized_stops;

public class and_a_transient_failure_then_recovers : given.a_job_storage_that_retries
{
    int _attempts;
    Catch<IImmutableList<JobState>> _jobs;

    void Establish() => _collection.FindAsync(
        Arg.Any<FilterDefinition<JobState>>(),
        Arg.Any<FindOptions<JobState, BsonDocument>>(),
        Arg.Any<CancellationToken>()).Returns(_ => ++_attempts <= 2
            ? Task.FromException<IAsyncCursor<BsonDocument>>(new MongoWaitQueueFullException("full"))
            : Task.FromResult(Cursor([_firstJob.ToBsonDocument()])));

    async Task Because() => _jobs = await _storage.GetJobsExcludingFinalizedStops(JobStatus.Stopped);

    [Fact] void should_return_the_jobs() => _jobs.AsT0.Count.ShouldEqual(1);
    [Fact] void should_read_until_it_succeeds() => _attempts.ShouldEqual(3);
    [Fact] void should_wait_between_attempts() => _time.Delays.Count.ShouldEqual(2);
}
