// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_live_jobs_hook_throws : given.a_running_job_with_two_steps
{
    Result<ResumeJobSuccess, ResumeJobError> _result;

    void Establish() => _job.BeforeResuming = () => Task.FromException(new InvalidOperationException("Resume was rejected"));

    async Task Because()
    {
        _result = await _job.Resume();
        await _job.OnStepSucceeded(_firstId, JobStepResult.Succeeded());
    }

    [Fact] void should_report_failure() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_preserve_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_preserve_tracking_for_the_live_step_report() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_not_start_the_first_step_again() => _first.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
    [Fact] void should_not_start_the_second_step_again() => _second.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
}
