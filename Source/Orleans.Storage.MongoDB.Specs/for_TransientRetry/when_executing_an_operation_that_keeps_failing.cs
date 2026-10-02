// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Storage.MongoDB.for_TransientRetry;

public class when_executing_an_operation_that_keeps_failing : given.a_transient_retry
{
    global::MongoDB.Driver.MongoWaitQueueFullException _exception;
    Exception _thrown;

    void Establish() => _exception = WaitQueueFull();

    async Task Because()
    {
        try
        {
            await _retry.Execute<string>("operation", () =>
            {
                _attempts++;
                throw _exception;
            });
        }
        catch (Exception ex)
        {
            _thrown = ex;
        }
    }

    [Fact] void should_surface_the_original_exception() => _thrown.ShouldEqual(_exception);
    [Fact] void should_attempt_the_first_time_and_every_retry() => _attempts.ShouldEqual(4);
}
