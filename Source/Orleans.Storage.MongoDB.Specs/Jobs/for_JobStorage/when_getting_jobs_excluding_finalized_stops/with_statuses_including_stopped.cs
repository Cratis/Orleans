// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStorage.when_getting_jobs_excluding_finalized_stops;

public class with_statuses_including_stopped : given.a_job_storage
{
    Catch<IImmutableList<JobState>> _result;

    void Establish() => WithDocuments([_firstJob.ToBsonDocument()]);
    async Task Because() => _result = await _storage.GetJobsExcludingFinalizedStops(JobStatus.Running, JobStatus.Stopped, JobStatus.Removing);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();

    [Fact] void should_leave_finalized_stops_out_in_storage() => _collection.Received(1).FindAsync(Arg.Is<FilterDefinition<JobState>>(filter => Render(filter).Equals(Render(ExpectedFilter()))), Arg.Any<FindOptions<JobState, BsonDocument>>(), Arg.Any<CancellationToken>());

    [Fact] void should_read_in_creation_order_without_a_limit() => _collection.Received(1).FindAsync(Arg.Any<FilterDefinition<JobState>>(), Arg.Is<FindOptions<JobState, BsonDocument>>(options => options.Skip == 0 && options.Limit == null && options.Sort != null), Arg.Any<CancellationToken>());

    static BsonDocument Render(FilterDefinition<JobState> filter) =>
        filter.Render(new RenderArgs<JobState>(BsonSerializer.LookupSerializer<JobState>(), BsonSerializer.SerializerRegistry));

    static FilterDefinition<JobState> ExpectedFilter() =>
        JobStorage.StatusFilter<JobState>([JobStatus.Running, JobStatus.Removing]) |
        (Builders<JobState>.Filter.Eq(new StringFieldDefinition<JobState, JobStatus>("status"), JobStatus.Stopped) &
         Builders<JobState>.Filter.Ne(new StringFieldDefinition<JobState, bool>("stopFinalized"), true));
}
