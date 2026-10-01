// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStepStorage.when_listing_steps;

public class with_unknown_types_in_both_collections : Jobs.given.a_database
{
    JobStepStorage _storage;
    JobId _jobId;
    JobStepState _runningStep;
    JobStepState _failedStep;
    Catch<IImmutableList<JobStepState>> _result;

    void Establish()
    {
        _storage = new JobStepStorage(_database);
        _jobId = JobId.New();
        _runningStep = new JobStepState { Id = new(_jobId, JobStepId.New()), Type = typeof(IJobStep), Status = JobStepStatus.Running };
        _failedStep = new JobStepState { Id = new(_jobId, JobStepId.New()), Type = typeof(IJobStep), Status = JobStepStatus.Failed };
        var unknownStep = _runningStep.ToBsonDocument();
        unknownStep["type"] = "RemovedStep, RemovedAssembly";
        var collection = Collection<JobStepState>(WellKnownCollectionNames.JobSteps);
        var failedCollection = Collection<JobStepState>(WellKnownCollectionNames.FailedJobSteps);
        collection.FindAsync(Arg.Any<FilterDefinition<JobStepState>>(), Arg.Any<FindOptions<JobStepState, BsonDocument>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Cursor([unknownStep, _runningStep.ToBsonDocument()], [unknownStep]));
        failedCollection.FindAsync(Arg.Any<FilterDefinition<JobStepState>>(), Arg.Any<FindOptions<JobStepState, BsonDocument>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Cursor([unknownStep, _failedStep.ToBsonDocument()]));
    }

    async Task Because() => _result = await _storage.GetForJob(_jobId);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_include_only_resolvable_steps_from_both_collections() => _result.AsT0.Select(step => step.Id).ShouldContainOnly(_runningStep.Id, _failedStep.Id);
}
