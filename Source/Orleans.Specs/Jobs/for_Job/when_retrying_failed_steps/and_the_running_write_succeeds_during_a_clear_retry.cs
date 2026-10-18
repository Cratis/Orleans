// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_retrying_failed_steps;

public class and_the_running_write_succeeds_during_a_clear_retry : given.a_job_with_completion_recovery
{
    async Task Establish()
    {
        _job.ShouldBeRemovedAfterCompletedWithFailures = true;
        _failClears = true;
        await _job.Resume();
    }

    async Task Because()
    {
        _result = await _job.RetryFailed();
        CountStepsInStorage((JobStepStatus.Running, 1));
        _failClears = false;
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_reopen_the_job() => _result.AsT0.ShouldEqual(1);
    [Fact] void should_persist_the_running_transition() => _storedStatus.ShouldEqual(JobStatus.Running);
    [Fact] void should_preserve_the_order_of_durable_transitions() => _writtenStatuses.ShouldEqual([JobStatus.CompletedWithFailures, JobStatus.Running]);
    [Fact] void should_cancel_the_previous_completions_clear_retry() => _clearAttempts.ShouldEqual(1);
    [Fact] void should_run_the_failed_step_once() => _failed.Verify(step => step.Retry(It.IsAny<GrainId>(), 2), Times.Once);
    [Fact] void should_not_repeat_the_old_completion_hook() => _job.AllStepsCompletedCount.ShouldEqual(1);
}
