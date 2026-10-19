// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_finishing_the_removal_fails;

public class and_it_keeps_failing : given.a_job_whose_removal_could_not_be_finished
{
    async Task Because()
    {
        for (var retry = 0; retry < 8; retry++)
        {
            await _silo.TimerRegistry.FireAllAsync();
        }
    }

    [Fact] void should_double_the_delay_up_to_one_minute() => TimerOptions.Select(options => options.DueTime.TotalSeconds).ShouldEqual([1, 2, 4, 8, 16, 32, 60, 60, 60]);
    [Fact] void should_keep_only_one_retry_armed() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(1);
}
