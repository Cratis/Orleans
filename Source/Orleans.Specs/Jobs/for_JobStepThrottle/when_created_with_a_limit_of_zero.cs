// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Orleans.Jobs.for_JobStepThrottle;

public class when_created_with_a_limit_of_zero : Specification
{
    JobStepThrottle _throttle;
    Task _first;
    Task _second;

    void Establish() => _throttle = new JobStepThrottle(0, NullLogger<JobStepThrottle>.Instance);

    void Because()
    {
        _first = _throttle.AcquireAsync();
        _second = _throttle.AcquireAsync();
    }

    [Fact] void should_clamp_to_one_slot_for_the_first_caller() => _first.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_make_the_second_caller_wait() => _second.IsCompleted.ShouldBeFalse();
}
