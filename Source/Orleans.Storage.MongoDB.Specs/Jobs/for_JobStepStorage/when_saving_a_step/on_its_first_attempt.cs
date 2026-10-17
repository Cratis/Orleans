// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using MongoDB.Driver;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStepStorage.when_saving_a_step;

public class on_its_first_attempt : Jobs.given.a_database
{
    JobStepStorage _storage;
    IMongoCollection<JobStepState> _failedCollection;
    JobStepState _state;

    void Establish()
    {
        _storage = new JobStepStorage(_database);
        Collection<JobStepState>(WellKnownCollectionNames.JobSteps);
        _failedCollection = Collection<JobStepState>(WellKnownCollectionNames.FailedJobSteps);
        _state = new() { Id = new(JobId.New(), JobStepId.New()), Type = typeof(IJobStep), Status = JobStepStatus.Running };
    }

    Task Because() => _storage.Save(_state.Id.JobId, _state.Id.JobStepId, _state);

    [Fact] void should_not_look_in_the_failed_collection() => _failedCollection.DidNotReceive().DeleteOneAsync(Arg.Any<FilterDefinition<JobStepState>>(), Arg.Any<CancellationToken>());
}
