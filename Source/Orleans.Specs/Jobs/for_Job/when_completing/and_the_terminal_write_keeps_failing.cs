// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_completing;

public class and_the_terminal_write_keeps_failing : given.a_job_ready_to_complete
{
    async Task Because()
    {
        _failWrites = true;
        await _job.Resume();
        for (var attempt = 0; attempt < 8; attempt++)
        {
            await _silo.TimerRegistry.FireAllAsync();
        }
    }

    [Fact] void should_back_off_and_cap_the_delay_at_a_minute() => TimerOptions.Select(options => options.DueTime.TotalSeconds).ShouldEqual([1, 2, 4, 8, 16, 32, 60, 60, 60]);
    [Fact] void should_keep_retrying_the_write() => _writeAttempts.ShouldEqual(9);
    [Fact] void should_leave_only_one_retry_armed() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(1);
    [Fact] void should_not_repeat_the_hook() => _job.AllStepsCompletedCount.ShouldEqual(1);
    [Fact] void should_not_clear_state() => _clearAttempts.ShouldEqual(0);
    [Fact] void should_not_deactivate() => Deactivations.ShouldEqual(0);
}
