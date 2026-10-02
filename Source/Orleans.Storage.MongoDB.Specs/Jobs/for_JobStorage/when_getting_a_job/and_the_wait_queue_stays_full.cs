// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStorage.when_getting_a_job;

public class and_the_wait_queue_stays_full : given.a_job_storage_that_retries
{
    MongoWaitQueueFullException _exception;
    Exception _error;

    void Establish()
    {
        _exception = new("full");
        FailReadsWith(() => _exception, int.MaxValue);
    }

    async Task Because() => _result = await _storage.GetJob(_firstJob.Id);

    [Fact] void should_report_the_original_exception() => (_result.TryGetException(out _error) && ReferenceEquals(_error, _exception)).ShouldBeTrue();
    [Fact] void should_attempt_the_first_time_and_every_retry() => _reads.ShouldEqual(4);
}
