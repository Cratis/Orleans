// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_stopped_jobs_hook_succeeds_on_retry : given.a_stopped_job_rejecting_resume
{
    bool _firstFailed;
    JobStatus _statusAfterFailure;

    async Task Because()
    {
        _firstFailed = !(await _job.Resume()).IsSuccess;
        _statusAfterFailure = _job.CurrentState.Status;
        _job.BeforeResuming = () => Task.CompletedTask;
        _result = await _job.Resume();
    }

    [Fact] void should_report_the_first_failure() => _firstFailed.ShouldBeTrue();
    [Fact] void should_be_stopped_after_failure() => _statusAfterFailure.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_resume_on_retry() => ((ResumeJobSuccess)_result).ShouldEqual(ResumeJobSuccess.Success);
    [Fact] void should_run_the_hook_again() => _job.BeforeResumingCount.ShouldEqual(2);
    [Fact] void should_start_the_step_once() => _step.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
    [Fact] void should_reset_stopped_progress() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(0);
    [Fact] void should_be_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
