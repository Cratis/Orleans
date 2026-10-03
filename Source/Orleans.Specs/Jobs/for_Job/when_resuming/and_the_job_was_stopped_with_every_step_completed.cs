// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

/// <summary>
/// Regression for https://github.com/Cratis/Chronicle/issues/4363 - a job stopped after its last step had already
/// finished stayed Stopped when resumed while reporting itself as completed, so it never ran its completion and
/// whatever waited on it waited forever.
/// </summary>
public class and_the_job_was_stopped_with_every_step_completed : given.the_job
{
    JobStepId _jobStepId;
    Mock<given.ISomeJobStep> _jobStep;
    Result<ResumeJobSuccess, ResumeJobError> _result;
    JobStatus _statusBeforeResuming;

    void Establish()
    {
        _job.ShouldBeResumable = true;
        _jobStepId = Guid.Parse("4f4f4f4f-0000-0000-0000-000000000004");
        _jobStep = AddJobStep(_jobStepId);

        // Already started is a successful start the harness can take without subscribing to a probe.
        _jobStep.Setup(_ => _.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Failed(StartJobStepError.AlreadyStarted));
    }

    async Task Because()
    {
        await _job.Start(new());

        // The client disconnected: the job was stopped, and its in-flight step then ended as failed - which
        // leaves the job Stopped with every step accounted for.
        _job.CurrentState.Status = JobStatus.Stopped;
        _job.CurrentState.Progress.TotalSteps = 1;
        _job.CurrentState.Progress.SuccessfulSteps = 0;
        _job.CurrentState.Progress.FailedSteps = 0;
        StoredJobStep(_jobStepId).Status = JobStepStatus.CompletedWithFailure;
        await _job.OnStepFailed(_jobStepId, JobStepResult.Failed("Subscriber is disconnected"));
        _statusBeforeResuming = _job.CurrentState.Status;
        _job.AllStepsCompletedCount = 0;

        _result = await _job.Resume();
    }

    [Fact] void should_have_left_the_job_stopped_before_resuming() => _statusBeforeResuming.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_report_the_job_as_completed() => ((ResumeJobSuccess)_result).ShouldEqual(ResumeJobSuccess.JobIsCompleted);
    [Fact] void should_not_leave_the_job_stopped() => _job.CurrentState.Status.ShouldNotEqual(JobStatus.Stopped);
    [Fact] void should_complete_the_job_with_failures() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedWithFailures);
    [Fact] void should_run_the_completion() => _job.AllStepsCompletedCount.ShouldEqual(1);
}
