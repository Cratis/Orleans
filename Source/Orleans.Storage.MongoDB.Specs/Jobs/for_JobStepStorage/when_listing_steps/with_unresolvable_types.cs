// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStepStorage.when_listing_steps;

public class with_unresolvable_types : Jobs.given.a_database
{
    JobStepStorage _storage;
    JobId _jobId;
    JobStepState[] _steps;
    Catch<IImmutableList<JobStepState>> _result;

    void Establish()
    {
        _storage = new JobStepStorage(_database);
        _jobId = JobId.New();
        _steps =
        [
            new() { Id = new(_jobId, JobStepId.New()), Type = typeof(IJobStep), Status = JobStepStatus.Running },
            new() { Id = new(_jobId, JobStepId.New()), Type = new JobStepType("RemovedStep, RemovedAssembly"), Status = JobStepStatus.Running },
            new() { Id = new(_jobId, JobStepId.New()), Type = new JobStepType("RemovedStep, RemovedAssembly"), Status = JobStepStatus.Failed }
        ];

        // Real BSON round-trips demonstrate that a step's type string does not need to resolve during storage reads.
        _steps = _steps.Select(step => BsonSerializer.Deserialize<JobStepState>(step.ToBsonDocument())).ToArray();
        WithSteps(WellKnownCollectionNames.JobSteps, _steps[..2]);
        WithSteps(WellKnownCollectionNames.FailedJobSteps, _steps[2..]);
    }

    async Task Because() => _result = await _storage.GetForJob(_jobId);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_include_every_step_from_both_collections() => _result.AsT0.Select(step => step.Id).ShouldContainOnly(_steps.Select(step => step.Id));
    [Fact] void should_preserve_unresolvable_type_names() => _result.AsT0.Count(step => step.Type.Value == "RemovedStep, RemovedAssembly").ShouldEqual(2);

    void WithSteps(string collectionName, JobStepState[] steps)
    {
        var collection = Collection<JobStepState>(collectionName);
        var cursor = Substitute.For<IAsyncCursor<JobStepState>>();
        cursor.Current.Returns(steps);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true, false);
        collection.FindAsync(Arg.Any<FilterDefinition<JobStepState>>(), Arg.Any<FindOptions<JobStepState, JobStepState>>(), Arg.Any<CancellationToken>()).Returns(cursor);
    }
}
