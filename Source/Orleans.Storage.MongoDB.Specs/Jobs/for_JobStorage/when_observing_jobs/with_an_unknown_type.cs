// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStorage.when_observing_jobs;

[Collection("MongoDBJobObservations")]
public class with_an_unknown_type : given.an_observed_job_storage
{
    async Task Because()
    {
        await ObserveInitialJobs();
        await InsertUnknownJob();
        await InsertRegisteredJob();
    }

    [Fact] void should_skip_unknown_jobs_in_the_initial_snapshot() => _initial.Select(job => job.Id).ShouldContainOnly(_firstJob.Id, _secondJob.Id);
    [Fact] void should_exclude_unknown_jobs_from_the_total() => _initialTotal.ShouldEqual(2);
    [Fact] void should_keep_visible_jobs_after_an_unknown_insert() => _afterUnknown.Select(job => job.Id).ShouldContainOnly(_firstJob.Id, _secondJob.Id);
    [Fact] void should_keep_the_total_after_an_unknown_insert() => _afterUnknownTotal.ShouldEqual(2);
    [Fact] void should_continue_observing_registered_jobs() => _afterKnown.Select(job => job.Id).ShouldContainOnly(_firstJob.Id, _secondJob.Id, _thirdJob.Id);
    [Fact] void should_count_a_registered_insert() => _afterKnownTotal.ShouldEqual(3);
    [Fact] void should_deserialize_registered_requests() => _afterKnown.Select(job => job.Request).ShouldContainOnly(_firstJob.Request, _secondJob.Request, _thirdJob.Request);
    [Fact] void should_filter_the_query_by_persisted_type_name_and_status() => _collection.Received(1).FindAsync(
        Arg.Is<FilterDefinition<JobState>>(filter => Render(filter).Equals(new BsonDocument
        {
            { "type", new BsonDocument("$in", new BsonArray { "custom-job-name" }) },
            { "$or", new BsonArray { new BsonDocument("status", (int)JobStatus.Running) } }
        })),
        Arg.Any<FindOptions<JobState, JobState>>(),
        Arg.Any<CancellationToken>());
}
