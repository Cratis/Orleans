// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_retrying_failed_steps;

public class and_the_running_write_fails_after_durable_completion : given.a_job_with_completion_recovery
{
    JobStatus _statusAfterRefusal;
    int _failedStepsAfterRefusal;

    async Task Establish()
    {
        _job.ShouldBeRemovedAfterCompletedWithFailures = true;
        _failClears = true;
        await _job.Resume();
        _failWrites = true;
    }

    async Task Because()
    {
        _result = await _job.RetryFailed();
        _statusAfterRefusal = _job.CurrentState.Status;
        _failedStepsAfterRefusal = _job.CurrentState.Progress.FailedSteps;
        _failWrites = false;
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_report_the_failed_reopening() => _result.AsT1.ShouldEqual(RetryFailedStepsError.CouldNotPersist);
    [Fact] void should_restore_the_terminal_status_in_memory() => _statusAfterRefusal.ShouldEqual(JobStatus.CompletedWithFailures);
    [Fact] void should_restore_the_failed_step_count() => _failedStepsAfterRefusal.ShouldEqual(1);
    [Fact] void should_leave_the_durable_record_terminal() => _storedStatus.ShouldEqual(JobStatus.CompletedWithFailures);
    [Fact] void should_not_persist_a_running_record() => _writtenStatuses.ShouldContainOnly(JobStatus.CompletedWithFailures);
    [Fact] void should_attempt_the_running_write_once() => _writeAttempts.ShouldEqual(2);
    [Fact] void should_preserve_the_pending_clear_retry() => _clearAttempts.ShouldEqual(2);
    [Fact] void should_not_repeat_the_completion_hook() => _job.AllStepsCompletedCount.ShouldEqual(1);
    [Fact] void should_not_run_the_failed_step() => _failed.Verify(step => step.Retry(It.IsAny<GrainId>(), It.IsAny<int>()), Times.Never);
    [Fact] void should_leave_no_finalization_timer_armed() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
