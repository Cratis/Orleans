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

public class with_no_statuses : given.a_job_storage
{
    Catch<IImmutableList<JobState>> _result;

    void Establish() => WithDocuments([_firstJob.ToBsonDocument()]);
    async Task Because() => _result = await _storage.GetJobsExcludingFinalizedStops();

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();

    [Fact] void should_leave_finalized_stops_out_of_every_job() => _collection.Received(1).FindAsync(Arg.Is<FilterDefinition<JobState>>(filter => Render(filter).Equals(Render(ExpectedFilter()))), Arg.Any<FindOptions<JobState, BsonDocument>>(), Arg.Any<CancellationToken>());

    static BsonDocument Render(FilterDefinition<JobState> filter) =>
        filter.Render(new RenderArgs<JobState>(BsonSerializer.LookupSerializer<JobState>(), BsonSerializer.SerializerRegistry));

    static FilterDefinition<JobState> ExpectedFilter() =>
        Builders<JobState>.Filter.Ne(new StringFieldDefinition<JobState, JobStatus>("status"), JobStatus.Stopped) |
        Builders<JobState>.Filter.Ne(new StringFieldDefinition<JobState, bool>("stopFinalized"), true);
}
