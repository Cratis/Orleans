// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_retrying_failed_steps;

public class and_terminal_persistence_is_pending : given.a_job_with_completion_recovery
{
    int _writesAfterRefusal;
    int _timersAfterRefusal;
    int _deactivationsAfterRefusal;

    async Task Establish()
    {
        _failWrites = true;
        await _job.Resume();
    }

    async Task Because()
    {
        _result = await _job.RetryFailed();
        _writesAfterRefusal = _writeAttempts;
        _timersAfterRefusal = _silo.TimerRegistry.NumberOfActiveTimers;
        _deactivationsAfterRefusal = Deactivations;
        _failWrites = false;
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_refuse_the_retry_with_the_persistence_error() => _result.AsT1.ShouldEqual(RetryFailedStepsError.CouldNotPersist);
    [Fact] void should_not_attempt_a_running_write() => _writesAfterRefusal.ShouldEqual(1);
    [Fact] void should_keep_the_terminal_retry_armed() => _timersAfterRefusal.ShouldEqual(1);
    [Fact] void should_not_deactivate_before_terminal_recovery() => _deactivationsAfterRefusal.ShouldEqual(0);
    [Fact] void should_persist_the_terminal_status_on_the_timer() => _writtenStatuses.ShouldContainOnly(JobStatus.CompletedWithFailures);
    [Fact] void should_retry_the_terminal_write_once() => _writeAttempts.ShouldEqual(2);
    [Fact] void should_not_repeat_the_completion_hook() => _job.AllStepsCompletedCount.ShouldEqual(1);
    [Fact] void should_not_run_the_failed_step() => _failed.Verify(step => step.Retry(It.IsAny<GrainId>(), It.IsAny<int>()), Times.Never);
    [Fact] void should_deactivate_after_terminal_recovery() => Deactivations.ShouldEqual(1);
}
