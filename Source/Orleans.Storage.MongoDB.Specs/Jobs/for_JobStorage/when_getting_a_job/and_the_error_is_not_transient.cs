// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStorage.when_getting_a_job;

public class and_the_error_is_not_transient : given.a_job_storage_that_retries
{
    void Establish() => FailReadsWith(() => new InvalidOperationException("bad"), int.MaxValue);

    async Task Because() => _result = await _storage.GetJob(_firstJob.Id);

    [Fact] void should_attempt_only_once() => _reads.ShouldEqual(1);
    [Fact] void should_not_wait() => _time.Delays.ShouldBeEmpty();
}
