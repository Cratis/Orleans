// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStorage;

public class when_getting_a_page_of_jobs : given.a_job_storage
{
    readonly DateTimeOffset _cutoff = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
    readonly JobQuery _query;
    Catch<IImmutableList<JobState>> _result;

    public when_getting_a_page_of_jobs() => _query = new()
    {
        Type = KnownJobType,
        Statuses = [JobStatus.CompletedWithFailures, JobStatus.Failed],
        CreatedBefore = _cutoff,
        Skip = 10,
        Take = 25
    };

    void Establish() => WithDocuments([_firstJob.ToBsonDocument()]);
    async Task Because() => _result = await _storage.GetJobs(_query);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();

    [Fact] void should_apply_type_status_and_retention_filters_in_storage() => _collection.Received(1).FindAsync(
        Arg.Is<FilterDefinition<JobState>>(filter => filter.Render(new RenderArgs<JobState>(BsonSerializer.LookupSerializer<JobState>(), BsonSerializer.SerializerRegistry)).Equals(
            ExpectedFilter().Render(new RenderArgs<JobState>(BsonSerializer.LookupSerializer<JobState>(), BsonSerializer.SerializerRegistry)))),

        Arg.Any<FindOptions<JobState, BsonDocument>>(),
        Arg.Any<CancellationToken>());

    [Fact] void should_apply_paging_in_storage() => _collection.Received(1).FindAsync(
        Arg.Any<FilterDefinition<JobState>>(),
        Arg.Is<FindOptions<JobState, BsonDocument>>(options =>
            options.Skip == _query.Skip &&
            options.Limit == _query.Take &&
            options.Sort != null),
        Arg.Any<CancellationToken>());

    FilterDefinition<JobState> ExpectedFilter() =>
        JobStorage.StatusFilter<JobState>(_query.Statuses) &
        Builders<JobState>.Filter.Eq(new StringFieldDefinition<JobState, JobType>("type"), KnownJobType) &
        Builders<JobState>.Filter.Lt(new StringFieldDefinition<JobState, DateTimeOffset>("created"), _cutoff);
}
