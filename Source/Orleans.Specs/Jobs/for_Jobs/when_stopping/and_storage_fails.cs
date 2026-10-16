// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Jobs.when_stopping;

public class and_storage_fails : given.jobs
{
    readonly Exception _failure = new("storage unavailable");
    Exception? _error;
    void Establish() => _storage.GetJobType(_id).Returns(Task.FromResult<Cratis.Monads.Catch<JobType, Storage.Jobs.JobError>>(_failure));
    async Task Because() => _error = await Catch.Exception(() => _jobs.Stop(_key, _id));

    [Fact] void should_propagate_the_storage_exception() => _error.ShouldBeSame(_failure);
    [Fact] void should_not_stop_the_job() => _job.DidNotReceive().Stop();
}
