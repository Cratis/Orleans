// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Orleans.Jobs.for_JobStepThrottle;

public class when_created_with_an_explicit_limit : Specification
{
    JobStepThrottle _throttle;
    Task[] _acquired;
    Task _beyondTheLimit;
    bool _waitedBeyondTheLimit;

    void Establish() => _throttle = new JobStepThrottle(3, NullLogger<JobStepThrottle>.Instance);

    async Task Because()
    {
        _acquired = [_throttle.AcquireAsync(), _throttle.AcquireAsync(), _throttle.AcquireAsync()];
        _beyondTheLimit = _throttle.AcquireAsync();
        _waitedBeyondTheLimit = !_beyondTheLimit.IsCompleted;
        _throttle.Release();
        await _beyondTheLimit.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_grant_as_many_slots_as_the_limit() => _acquired.All(task => task.IsCompletedSuccessfully).ShouldBeTrue();
    [Fact] void should_make_the_next_caller_wait() => _waitedBeyondTheLimit.ShouldBeTrue();
    [Fact] void should_grant_the_waiting_caller_when_a_slot_is_released() => _beyondTheLimit.IsCompletedSuccessfully.ShouldBeTrue();
}
