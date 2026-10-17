// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_interrupted_jobs_hook_succeeds_on_retry : given.an_interrupted_job_rejecting_resume
{
    bool _firstFailed;
    JobStatus _statusBeforeRetry;

    async Task Because()
    {
        _firstFailed = !(await _job.Resume()).IsSuccess;
        _statusBeforeRetry = _job.CurrentState.Status;
        _job.BeforeResuming = () => Task.CompletedTask;
        _result = await _job.Resume();
    }

    [Fact] void should_report_the_hook_failure() => _firstFailed.ShouldBeTrue();
    [Fact] void should_preserve_running_before_retry() => _statusBeforeRetry.ShouldEqual(JobStatus.Running);
    [Fact] void should_retry_recovery() => ((ResumeJobSuccess)_result).ShouldEqual(ResumeJobSuccess.Success);
    [Fact] void should_restart_the_interrupted_step() => _unfinished.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
    [Fact] void should_run_the_hook_again() => _job.BeforeResumingCount.ShouldEqual(2);
    [Fact] void should_be_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
