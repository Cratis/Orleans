// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_completing;

public class and_a_retained_jobs_terminal_write_fails : given.a_job_ready_to_complete
{
    async Task Because()
    {
        _job.ShouldBeRemovedAfterCompleted = false;
        _failWrites = true;
        await _job.Resume();
        _failWrites = false;
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_persist_the_terminal_status_on_retry() => _writtenStatuses.ShouldContainOnly(JobStatus.CompletedSuccessfully);
    [Fact] void should_not_write_again_after_success() => _writeAttempts.ShouldEqual(2);
    [Fact] void should_not_clear_the_retained_state() => _clearAttempts.ShouldEqual(0);
    [Fact] void should_not_repeat_the_completion_hook() => _job.AllStepsCompletedCount.ShouldEqual(1);
    [Fact] void should_deactivate_after_persisting() => Deactivations.ShouldEqual(1);
}
