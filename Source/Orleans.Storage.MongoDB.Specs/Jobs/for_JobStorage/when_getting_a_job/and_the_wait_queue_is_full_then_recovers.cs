// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStorage.when_getting_a_job;

public class and_the_wait_queue_is_full_then_recovers : given.a_job_storage_that_retries
{
    void Establish() => FailReadsWith(() => new MongoWaitQueueFullException("full"), 2);

    async Task Because() => _result = await _storage.GetJob(_firstJob.Id);

    [Fact] void should_return_the_job() => _result.TryGetResult(out _).ShouldBeTrue();
    [Fact] void should_read_until_it_succeeds() => _reads.ShouldEqual(3);
    [Fact] void should_wait_between_attempts() => _time.Delays.Count.ShouldEqual(2);
}
