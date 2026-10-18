// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_completing;

public class and_clearing_keeps_failing : given.a_job_ready_to_complete
{
    int _deactivationsBeforeRetry;

    async Task Because()
    {
        _failClears = true;
        await _job.Resume();
        await HandleCompletion();
        _deactivationsBeforeRetry = Deactivations;
        await _silo.TimerRegistry.FireAllAsync();
        await HandleCompletion();
    }

    [Fact] void should_leave_a_durable_terminal_record_for_retention() => _storedStatus.ShouldEqual(JobStatus.CompletedSuccessfully);
    [Fact] void should_only_clear_after_persisting_terminal_state() => _statusesAtClear.ShouldEqual(new[] { JobStatus.CompletedSuccessfully, JobStatus.CompletedSuccessfully });
    [Fact] void should_retry_the_clear_only_once() => _clearAttempts.ShouldEqual(2);
    [Fact] void should_not_rewrite_the_terminal_status() => _writeAttempts.ShouldEqual(1);
    [Fact] void should_not_repeat_the_completion_hook() => _job.AllStepsCompletedCount.ShouldEqual(1);
    [Fact] void should_not_deactivate_before_retrying() => _deactivationsBeforeRetry.ShouldEqual(0);
    [Fact] void should_deactivate_after_the_second_clear_attempt() => Deactivations.ShouldEqual(1);
    [Fact] void should_leave_no_retry_timer() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
