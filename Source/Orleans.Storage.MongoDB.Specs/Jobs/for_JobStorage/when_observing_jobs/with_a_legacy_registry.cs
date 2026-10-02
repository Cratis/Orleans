// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStorage.when_observing_jobs;

[Collection("MongoDBJobObservations")]
public class with_a_legacy_registry : given.an_observed_job_storage
{
    protected override bool HasCatalog => false;

    void Establish() => _unknownJob["request"] = new BsonInt32(42);

    async Task Because()
    {
        await ObserveInitialJobs();
        await InsertRegisteredJob();
    }

    [Fact] void should_skip_unknown_jobs_without_deserializing_their_requests() => _initial.Select(job => job.Id).ShouldContainOnly(_firstJob.Id, _secondJob.Id);
    [Fact] void should_continue_observing_registered_jobs() => _afterKnown.Select(job => job.Id).ShouldContainOnly(_firstJob.Id, _secondJob.Id, _thirdJob.Id);
    [Fact] void should_deserialize_registered_requests() => _afterKnown.Select(job => job.Request).ShouldContainOnly(_firstJob.Request, _secondJob.Request, _thirdJob.Request);
}
