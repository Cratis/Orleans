// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_completing;

public class and_clearing_succeeds_on_retry : given.a_job_ready_to_complete
{
    async Task Because()
    {
        _failClears = true;
        await _job.Resume();
        _failClears = false;
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_retry_the_clear_once() => _clearAttempts.ShouldEqual(2);
    [Fact] void should_write_the_terminal_status_only_once() => _writeAttempts.ShouldEqual(1);
    [Fact] void should_run_the_completion_hook_only_once() => _job.AllStepsCompletedCount.ShouldEqual(1);
    [Fact] void should_deactivate_after_clearing() => Deactivations.ShouldEqual(1);
    [Fact] void should_leave_no_retry_timer() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
