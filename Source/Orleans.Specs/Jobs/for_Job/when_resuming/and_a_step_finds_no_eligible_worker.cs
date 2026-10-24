// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs.Placement;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_a_step_finds_no_eligible_worker : given.a_job_with_a_throwing_resume_start
{
    Result<ResumeJobSuccess, ResumeJobError> _result;
    JobStatus _statusAfterResume;
    int _failedStepsAfterResume;

    void Establish() => _steps[0].Setup(step => step.Start(It.IsAny<GrainId>()))
        .ThrowsAsync(new NoEligibleJobWorker(GrainId.Create("some-job-step", "some-key"), "no silo has the worker role"));

    async Task Because()
    {
        _result = await _job.Resume();
        _statusAfterResume = _job.CurrentState.Status;
        _failedStepsAfterResume = _job.CurrentState.Progress.FailedSteps;

        // An eligible worker has joined.
        _steps[0].Setup(step => step.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Success());
        await _job.RunWatchdog();
    }

    [Fact] void should_report_only_that_step_as_failed_to_resume() => ((FailedResumingJobSteps)(ResumeJobError)_result).FailedJobSteps.ShouldContainOnly(_stepIds[0]);
    [Fact] void should_leave_the_job_running_after_the_resume() => _statusAfterResume.ShouldEqual(JobStatus.Running);
    [Fact] void should_not_count_the_step_as_failed() => _failedStepsAfterResume.ShouldEqual(0);
    [Fact] void should_try_the_step_again_once_on_the_next_watchdog_run() => _steps[0].Verify(step => step.Start(It.IsAny<GrainId>()), Times.Exactly(2));
    [Fact] void should_still_be_running_after_the_retry() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_still_not_have_failed_the_step_after_the_retry() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(0);
}
