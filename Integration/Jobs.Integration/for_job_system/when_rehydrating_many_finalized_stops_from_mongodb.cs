// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.MongoDB;
using Cratis.Orleans.Storage.MongoDB.Jobs;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Orleans.Jobs.Integration.for_job_system;

/// <summary>
/// Shows, against a real MongoDB, that the rehydration read leaves finalized stops out in storage instead of reading
/// every stopped job. Requires a MongoDB running on localhost:27017.
/// </summary>
/// <remarks>
/// The execution statistics are written to the test output so the CI log carries the numbers.
/// </remarks>
[Collection(JobsClusterCollection.Name)]
public class when_rehydrating_many_finalized_stops_from_mongodb : Specification
{
    const int FinalizedStops = 1000;
    const int PendingStops = 10;
    const int RunningJobs = 5;

    static readonly JobStatus[] _rehydratedStatuses =
    [
        JobStatus.Running,
        JobStatus.PreparingJob,
        JobStatus.PreparingSteps,
        JobStatus.StartingSteps,
        JobStatus.Stopped,
        JobStatus.Removing
    ];

    IMongoDatabase _database = null!;
    IMongoCollection<BsonDocument> _collection = null!;
    IImmutableList<JobState> _jobs = ImmutableList<JobState>.Empty;
    BsonDocument _newStats = new();
    BsonDocument _oldStats = new();

    async Task Establish()
    {
        var fixture = JobsClusterFixture.Shared;
        JobsClusterFixture.ResetStorage();
        var jobTypes = fixture.Services.GetRequiredService<IJobTypes>();
        _database = new MongoClient(JobsClusterFixture.ConnectionString).GetDatabase(JobsClusterFixture.DatabaseName);
        _collection = _database.GetCollection<BsonDocument>(WellKnownCollectionNames.Jobs);

        // A fresh storage ensures the indexes on its first read, which the reset above just dropped.
        var storage = new JobStorage(_database, jobTypes);
        var type = jobTypes.GetFor(typeof(IIntegrationJob)).AsT0;
        var template = new JobState { Id = JobId.New(), Type = type, Status = JobStatus.Running, Request = new IntegrationJobRequest(["item"]) };
        (await storage.Save(template.Id, template)).IsSuccess.ShouldBeTrue();
        var document = await _collection.Find(new BsonDocument("_id", new BsonBinaryData(template.Id.Value, GuidRepresentation.Standard))).SingleAsync();
        await _collection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);

        var documents = Enumerable.Range(0, FinalizedStops + PendingStops + RunningJobs).Select(index =>
        {
            var clone = document.DeepClone().AsBsonDocument;
            clone["_id"] = new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard);
            clone["status"] = (int)(index < FinalizedStops + PendingStops ? JobStatus.Stopped : JobStatus.Running);
            clone["stopFinalized"] = index < FinalizedStops;
            return clone;
        });
        await _collection.InsertManyAsync(documents);

        var read = await storage.GetJobsExcludingFinalizedStops(_rehydratedStatuses);
        _jobs = read.AsT0;

        var statuses = new BsonArray(_rehydratedStatuses.Cast<int>());
        var otherStatuses = new BsonArray(_rehydratedStatuses.Where(status => status != JobStatus.Stopped).Cast<int>());
        var newFilter = new BsonDocument("$or", new BsonArray
        {
            new BsonDocument("status", new BsonDocument("$in", otherStatuses)),
            new BsonDocument { { "status", (int)JobStatus.Stopped }, { "stopFinalized", new BsonDocument("$ne", true) } }
        });
        var oldFilter = new BsonDocument("status", new BsonDocument("$in", statuses));
        _newStats = await Explain(newFilter);
        _oldStats = await Explain(oldFilter);
        TestContext.Current.TestOutputHelper?.WriteLine($"new filter executionStats: {_newStats.ToJson()}");
        TestContext.Current.TestOutputHelper?.WriteLine($"old filter executionStats: {_oldStats.ToJson()}");
    }

    [Fact] void should_return_only_the_stops_and_running_jobs_that_still_need_rehydration() => _jobs.Count.ShouldEqual(PendingStops + RunningJobs);
    [Fact] void should_leave_every_finalized_stop_out_in_storage() => _newStats["nReturned"].ToInt32().ShouldEqual(PendingStops + RunningJobs);
    [Fact] void should_not_examine_the_finalized_stops() => _newStats["totalDocsExamined"].ToInt32().ShouldEqual(PendingStops + RunningJobs);
    [Fact] void should_have_returned_every_stopped_job_before() => _oldStats["nReturned"].ToInt32().ShouldEqual(FinalizedStops + PendingStops + RunningJobs);

    async Task<BsonDocument> Explain(BsonDocument filter)
    {
        var explained = await _database.RunCommandAsync<BsonDocument>(new BsonDocument
        {
            { "explain", new BsonDocument { { "find", WellKnownCollectionNames.Jobs }, { "filter", filter } } },
            { "verbosity", "executionStats" }
        });
        return explained["executionStats"].AsBsonDocument;
    }
}
