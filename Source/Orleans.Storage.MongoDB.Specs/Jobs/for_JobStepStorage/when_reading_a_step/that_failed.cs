// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using MongoDB.Driver;

using JobStepError = Cratis.Orleans.Storage.Jobs.JobStepError;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStepStorage.when_reading_a_step;

public class that_failed : Jobs.given.a_database
{
    JobStepStorage _storage;
    JobStepState _failed;
    Catch<JobStepState, JobStepError> _result;

    void Establish()
    {
        _storage = new JobStepStorage(_database);
        _failed = new() { Id = new(JobId.New(), JobStepId.New()), Type = typeof(IJobStep), Status = JobStepStatus.CompletedWithFailure, Attempt = 2 };
        WithSteps(WellKnownCollectionNames.JobSteps);
        WithSteps(WellKnownCollectionNames.FailedJobSteps, _failed);
    }

    async Task Because() => _result = await _storage.Read<JobStepState>(_failed.Id.JobId, _failed.Id.JobStepId);

    [Fact] void should_find_it_in_the_failed_collection() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_read_its_attempt() => _result.AsT0.Attempt.ShouldEqual(2);

    void WithSteps(string collectionName, params JobStepState[] steps)
    {
        var collection = Collection<JobStepState>(collectionName);
        var cursor = Substitute.For<IAsyncCursor<JobStepState>>();
        cursor.Current.Returns(steps);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(steps.Length > 0, false);
        collection.FindAsync(Arg.Any<FilterDefinition<JobStepState>>(), Arg.Any<FindOptions<JobStepState, JobStepState>>(), Arg.Any<CancellationToken>()).Returns(cursor);
    }
}
