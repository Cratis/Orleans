// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_completing;

public class and_the_completion_hook_fails : given.a_job_ready_to_complete
{
    async Task Because()
    {
        _job.OnCompletedThrows = true;
        _failWrites = true;
        await _job.Resume();
        await HandleCompletion();
        _failWrites = false;
        await _silo.TimerRegistry.FireAllAsync();
        await HandleCompletion();
    }

    [Fact] void should_preserve_and_persist_the_failed_status() => _writtenStatuses.ShouldContainOnly(JobStatus.Failed);
    [Fact] void should_record_the_hook_failure_once() => _job.CurrentState.StatusChanges.Count(change => change.Status is JobStatus.Failed).ShouldEqual(1);
    [Fact] void should_not_clear_the_failed_job() => _clearAttempts.ShouldEqual(0);
    [Fact] void should_not_repeat_the_failing_hook() => _job.AllStepsCompletedCount.ShouldEqual(1);
    [Fact] void should_deactivate_after_persisting() => Deactivations.ShouldEqual(1);
}
