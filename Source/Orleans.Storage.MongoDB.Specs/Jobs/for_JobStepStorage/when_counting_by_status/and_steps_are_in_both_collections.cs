// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStepStorage.when_counting_by_status;

public class and_steps_are_in_both_collections : Jobs.given.a_database
{
    JobStepStorage _storage;
    Catch<IReadOnlyDictionary<JobStepStatus, int>> _result;

    void Establish()
    {
        _storage = new JobStepStorage(_database);
        WithGroups(WellKnownCollectionNames.JobSteps, Group(JobStepStatus.Running, 3), Group(JobStepStatus.Unknown, 4));
        WithGroups(WellKnownCollectionNames.FailedJobSteps, Group(JobStepStatus.CompletedWithFailure, 2), Group(JobStepStatus.Failed, 1));
    }

    async Task Because() => _result = await _storage.CountByStatus(JobId.New());

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_count_the_running_steps() => _result.AsT0[JobStepStatus.Running].ShouldEqual(3);
    [Fact] void should_count_the_steps_not_started() => _result.AsT0[JobStepStatus.Unknown].ShouldEqual(4);
    [Fact] void should_count_the_steps_that_completed_with_failure() => _result.AsT0[JobStepStatus.CompletedWithFailure].ShouldEqual(2);
    [Fact] void should_count_the_unrecoverably_failed_steps() => _result.AsT0[JobStepStatus.Failed].ShouldEqual(1);

    static BsonDocument Group(JobStepStatus status, int count) => new() { { "_id", (int)status }, { "count", count } };

    void WithGroups(string collectionName, params BsonDocument[] groups)
    {
        var collection = Collection<JobStepState>(collectionName);
        var cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
        cursor.Current.Returns(groups);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true, false);
        collection.AggregateAsync(Arg.Any<PipelineDefinition<JobStepState, BsonDocument>>(), Arg.Any<AggregateOptions>(), Arg.Any<CancellationToken>()).Returns(cursor);
    }
}
