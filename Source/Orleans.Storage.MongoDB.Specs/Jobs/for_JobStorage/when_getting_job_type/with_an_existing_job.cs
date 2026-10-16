// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

using JobError = Cratis.Orleans.Storage.Jobs.JobError;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStorage.when_getting_job_type;

public class with_an_existing_job : given.a_job_storage
{
    Catch<JobType, JobError> _result;
    FindOptions<JobState, BsonDocument> _options;
    FilterDefinition<JobState> _filter;

    void Establish()
    {
        WithDocuments([new BsonDocument("type", KnownJobType.Value)]);
        _collection.FindAsync(Arg.Do<FilterDefinition<JobState>>(filter => _filter = filter), Arg.Do<FindOptions<JobState, BsonDocument>>(options => _options = options), Arg.Any<CancellationToken>()).Returns(_ => Cursor([new BsonDocument("type", KnownJobType.Value)]));
    }
    async Task Because() => _result = await _storage.GetJobType(_firstJob.Id);

    [Fact] void should_return_the_stored_type() => _result.AsT0.ShouldEqual(KnownJobType);
    [Fact] void should_only_read_the_type() => _options.Projection.Render(new RenderArgs<JobState>(BsonSerializer.LookupSerializer<JobState>(), BsonSerializer.SerializerRegistry)).Document.ShouldEqual(new BsonDocument { { "type", 1 }, { "_id", 0 } });
    [Fact] void should_read_one_record() => _options.Limit.ShouldEqual(1);
    [Fact] void should_select_the_requested_identity() => _filter.Render(new RenderArgs<JobState>(BsonSerializer.LookupSerializer<JobState>(), BsonSerializer.SerializerRegistry))["_id"].ShouldEqual(new BsonBinaryData(_firstJob.Id.Value, GuidRepresentation.Standard));
}
