// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_a_reported_outcome_has_stale_stopped_storage_during_retry : given.a_job_left_running_after_an_incomplete_stop_and_a_rejected_resume
{
    protected override int NumberOfSteps => 3;

    async Task Establish()
    {
        await Fail(1);
        StoredJobStep(_stepIds[1]).Status = JobStepStatus.Stopped;
        _steps[1].Invocations.Clear();
        AllowResume();
    }

    async Task Because()
    {
        await _job.Resume();
        await Fail(1);
    }

    [Fact] void should_recover_the_genuinely_stopped_step() => StartsOf(0).ShouldEqual(1);
    [Fact] void should_not_restart_the_counted_failure() => StartsOf(1).ShouldEqual(0);
    [Fact] void should_check_the_remaining_survivor() => StartsOf(2).ShouldEqual(1);
    [Fact] void should_keep_the_failure_count() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(1);
    [Fact] void should_keep_the_durable_outcome_record() => _job.CurrentState.CompletedStepsCounted.Count(id => id == _stepIds[1]).ShouldEqual(1);
    [Fact] void should_handle_the_failure_once() => _job.HandledStepResults.Count(id => id == _stepIds[1]).ShouldEqual(1);
    [Fact] void should_keep_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
