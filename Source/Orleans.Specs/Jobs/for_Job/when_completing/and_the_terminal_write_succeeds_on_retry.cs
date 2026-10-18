// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_completing;

public class and_the_terminal_write_succeeds_on_retry : given.a_job_ready_to_complete
{
    async Task Because()
    {
        _failWrites = true;
        await _job.Resume();
        _failWrites = false;
        await _silo.TimerRegistry.FireAllAsync();
        await HandleCompletion();
    }

    [Fact] void should_persist_the_completed_status() => _storedStatus.ShouldEqual(JobStatus.CompletedSuccessfully);
    [Fact] void should_retry_the_write_once() => _writeAttempts.ShouldEqual(2);
    [Fact] void should_clear_only_after_the_terminal_write_succeeds() => _statusesAtClear.ShouldContainOnly(JobStatus.CompletedSuccessfully);
    [Fact] void should_not_repeat_the_completion_hook() => _job.AllStepsCompletedCount.ShouldEqual(1);
    [Fact] void should_deactivate_after_clearing() => Deactivations.ShouldEqual(1);
    [Fact] void should_dispose_the_retry_timer() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
