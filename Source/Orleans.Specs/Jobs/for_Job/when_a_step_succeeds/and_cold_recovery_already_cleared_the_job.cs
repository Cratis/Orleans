// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_succeeds;

public class and_cold_recovery_already_cleared_the_job : given.a_job_finalized_by_cold_recovery
{
    async Task Because() => _report = await _job.OnStepSucceeded(_stepId, JobStepResult.Succeeded());

    [Fact] void should_have_finalized_during_recovery() => ((ResumeJobSuccess)_resume).ShouldEqual(ResumeJobSuccess.JobIsCompleted);
    [Fact] void should_acknowledge_the_late_report() => _report.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_recreate_the_job_record() => _silo.StorageStats<given.SomeJob, given.SomeJobState>().Writes.ShouldEqual(0);
    [Fact] void should_leave_progress_unchanged() => _job.CurrentState.Progress.ShouldEqual(_progressBeforeReport);
    [Fact] void should_leave_status_unchanged() => _job.CurrentState.Status.ShouldEqual(_statusBeforeReport);
    [Fact] void should_not_deliver_the_result_again() => _job.HandledStepResults.ShouldBeEmpty();
    [Fact] void should_only_complete_once() => _job.AllStepsCompletedCount.ShouldEqual(1);
    [Fact] void should_unsubscribe_the_reporting_step() => _observers.Count.ShouldEqual(0);
}
