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

public class when_listing_jobs_of_a_type : given.a_job_storage
{
    Catch<IImmutableList<JobState>, Storage.Jobs.JobError> _result;

    void Establish() => WithDocuments([_firstJob.ToBsonDocument()]);
    async Task Because() => _result = await _storage.GetJobs<IJob, JobState>(JobStatus.Running);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_return_the_registered_job() => _result.AsT0.Select(job => job.Id).ShouldContainOnly(_firstJob.Id);
    [Fact] void should_filter_by_type_and_status_in_the_database() => _collection.Received(1).FindAsync(
        Arg.Is<FilterDefinition<JobState>>(filter => filter.Render(new RenderArgs<JobState>(BsonSerializer.LookupSerializer<JobState>(), BsonSerializer.SerializerRegistry)).Equals(
            JobStorage.TypeAndStatusFilter<JobState>(KnownJobType, new[] { JobStatus.Running }).Render(new RenderArgs<JobState>(BsonSerializer.LookupSerializer<JobState>(), BsonSerializer.SerializerRegistry)))),
        Arg.Any<FindOptions<JobState, BsonDocument>>(),
        Arg.Any<CancellationToken>());
}
