// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming_if_running;

public class and_the_job_completed_with_failures : given.a_job_that_is_not_running
{
    protected override JobStatus Status => JobStatus.CompletedWithFailures;

    async Task Because() => _result = await _job.ResumeIfRunning();

    [Fact] void should_report_that_the_job_is_not_running() => ((ResumeJobSuccess)_result).ShouldEqual(ResumeJobSuccess.JobIsNotRunning);
    [Fact] void should_leave_the_status() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedWithFailures);
    [Fact] void should_not_change_history() => _job.CurrentState.StatusChanges.Count.ShouldEqual(_changesBefore);
    [Fact] void should_not_write() => Writes.ShouldEqual(_writesBefore);
    [Fact] void should_not_run_the_hook() => _job.BeforeResumingCount.ShouldEqual(0);
    [Fact] void should_not_start_a_step() => _unfinished.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
}
