// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_completing;

public class and_failed_steps_are_not_retained : given.a_job_ready_to_complete
{
    async Task Because()
    {
        _job.CurrentState.Progress.SuccessfulSteps = 0;
        _job.CurrentState.Progress.FailedSteps = 1;
        _job.ShouldBeRemovedAfterCompletedWithFailures = true;
        _failWrites = true;
        await _job.Resume();
        _failWrites = false;
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_persist_completion_with_failures() => _writtenStatuses.ShouldContainOnly(JobStatus.CompletedWithFailures);
    [Fact] void should_clear_only_after_the_terminal_write_succeeds() => _statusesAtClear.ShouldContainOnly(JobStatus.CompletedWithFailures);
    [Fact] void should_not_repeat_the_completion_hook() => _job.AllStepsCompletedCount.ShouldEqual(1);
    [Fact] void should_deactivate_after_clearing() => Deactivations.ShouldEqual(1);
}
