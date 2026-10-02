// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Storage.MongoDB.for_TransientRetry;

public class when_executing_an_operation_that_recovers : given.a_transient_retry
{
    string _result;

    async Task Because() => _result = await _retry.Execute("operation", () =>
    {
        _attempts++;
        return _attempts < 3 ? throw WaitQueueFull() : Task.FromResult("done");
    });

    [Fact] void should_return_the_result() => _result.ShouldEqual("done");
    [Fact] void should_attempt_until_success() => _attempts.ShouldEqual(3);
    [Fact] void should_back_off_exponentially() => _time.Delays.ShouldEqual([TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200)]);
}
