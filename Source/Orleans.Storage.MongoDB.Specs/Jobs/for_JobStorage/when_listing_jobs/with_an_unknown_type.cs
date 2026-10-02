// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using MongoDB.Bson;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStorage.when_listing_jobs;

public class with_an_unknown_type : given.a_job_storage
{
    Catch<IImmutableList<JobState>> _result;

    void Establish() => WithDocuments([_unknownJob, _firstJob.ToBsonDocument()], [_secondJob.ToBsonDocument(), _unknownJob]);
    async Task Because() => _result = await _storage.GetJobs(JobStatus.Running);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_return_both_registered_jobs() => _result.AsT0.Select(job => job.Id).ShouldContainOnly(_firstJob.Id, _secondJob.Id);
    [Fact] void should_deserialize_the_known_requests() => _result.AsT0.Select(job => job.Request).ShouldContainOnly(_firstJob.Request, _secondJob.Request);
    [Fact] void should_not_mutate_stored_jobs() => _collection.ReceivedCalls().Select(call => call.GetMethodInfo().Name).Intersect(["DeleteOneAsync", "DeleteManyAsync", "ReplaceOneAsync", "UpdateOne", "UpdateOneAsync", "UpdateMany", "UpdateManyAsync", "BulkWriteAsync"]).ShouldBeEmpty();
}
