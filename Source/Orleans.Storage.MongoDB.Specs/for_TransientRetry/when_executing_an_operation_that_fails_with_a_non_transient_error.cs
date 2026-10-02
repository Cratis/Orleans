// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Storage.MongoDB.for_TransientRetry;

public class when_executing_an_operation_that_fails_with_a_non_transient_error : given.a_transient_retry
{
    Exception _thrown;

    async Task Because()
    {
        try
        {
            await _retry.Execute<string>("operation", () =>
            {
                _attempts++;
                throw new InvalidOperationException("not transient");
            });
        }
        catch (Exception ex)
        {
            _thrown = ex;
        }
    }

    [Fact] void should_surface_the_exception() => _thrown.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_attempt_only_once() => _attempts.ShouldEqual(1);
    [Fact] void should_not_wait() => _time.Delays.ShouldBeEmpty();
}
