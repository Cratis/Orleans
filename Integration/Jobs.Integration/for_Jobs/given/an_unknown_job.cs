// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage;
using Cratis.Orleans.Storage.MongoDB;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Orleans.Jobs.Integration.for_Jobs.given;

public class an_unknown_job : Specification
{
    protected IJobs _jobs;
    protected readonly JobId _id = JobId.New();
    protected readonly JobKey _key = new(JobsClusterFixture.Scope, "tenant-a");
    protected readonly JobType _type = new("Future.Job");
    protected IMongoCollection<BsonDocument> _collection;
    protected BsonDocument _original;
    protected Cratis.Monads.Catch<JobType, Storage.Jobs.JobError> _storedType;
    protected Cratis.Monads.Catch<JobState, Storage.Jobs.JobError> _fullRead;

    async Task Establish()
    {
        var fixture = JobsClusterFixture.Shared;
        JobsClusterFixture.ResetStorage();
        _jobs = fixture.Services.GetRequiredService<IJobs>();
        _collection = new MongoClient(JobsClusterFixture.ConnectionString).GetDatabase(JobsClusterFixture.DatabaseName).GetCollection<BsonDocument>(WellKnownCollectionNames.Jobs);
        _original = new BsonDocument
        {
            { "_id", new BsonBinaryData(_id.Value, GuidRepresentation.Standard) },
            { "type", _type.Value },
            { "status", (int)JobStatus.Running },
            { "request", 42 }
        };
        await _collection.InsertOneAsync(_original);
        var storage = fixture.Services.GetRequiredService<IJobsStorage>().GetFor(_key.Scope, _key.Namespace).Jobs;
        _fullRead = await storage.GetJob(_id);
        _storedType = await storage.GetJobType(_id);
    }

    protected Task<BsonDocument> ReadStored() => _collection.Find(new BsonDocument("_id", _original["_id"])).SingleAsync();
}
