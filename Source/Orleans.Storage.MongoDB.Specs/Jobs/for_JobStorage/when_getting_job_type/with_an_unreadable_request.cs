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

public class with_an_unreadable_request : given.a_job_storage
{
    Catch<JobType, JobError> _result;
    Catch<JobState, JobError> _fullRead;

    void Establish()
    {
        _unknownJob["request"] = 42;
        WithDocuments([new BsonDocument("type", _unknownJob["type"])]);
        _collection.FindAsync(Arg.Any<FilterDefinition<JobState>>(), Arg.Any<FindOptions<JobState, JobState>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                BsonSerializer.Deserialize<JobState>(_unknownJob);
                return Task.FromResult(Substitute.For<IAsyncCursor<JobState>>());
            });
    }
    async Task Because()
    {
        _fullRead = await _storage.GetJob(_firstJob.Id);
        _result = await _storage.GetJobType(_firstJob.Id);
    }

    [Fact] void should_fail_to_read_the_request() => _fullRead.TryGetException(out _).ShouldBeTrue();
    [Fact] void should_still_return_the_stored_type() => _result.AsT0.ShouldEqual(new JobType(_unknownJob["type"].AsString));
}
