// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_request_is_missing_without_registered_admission : given.a_running_job_with_two_steps
{
    Result<ResumeJobSuccess, ResumeJobError> _result;
    int _resumeHooksBefore;
    int _startHooksBefore;
    int _dispatchesBefore;
    int _plansBefore;

    void Establish()
    {
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Stopped });
        _job.CurrentState.Status = JobStatus.Stopped;
        _job.ShouldBeResumable = true;
        _job.CurrentState.Request = null!;
        _resumeHooksBefore = _job.BeforeResumingCount;
        _startHooksBefore = _job.BeforeStartingCount;
        _dispatchesBefore = _job.DispatchContexts.Count;
        _plansBefore = _job.PrepareStepsCount;
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_resume_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_be_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_run_the_resume_hook_once() => _job.BeforeResumingCount.ShouldEqual(_resumeHooksBefore + 1);
    [Fact] void should_not_run_the_start_hook_again() => _job.BeforeStartingCount.ShouldEqual(_startHooksBefore);
    [Fact] void should_dispatch_once() => _job.DispatchContexts.Count.ShouldEqual(_dispatchesBefore + 1);
    [Fact] void should_not_plan_again() => _job.PrepareStepsCount.ShouldEqual(_plansBefore);
    [Fact] void should_restart_the_first_step_once() => _first.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Exactly(2));
    [Fact] void should_restart_the_second_step_once() => _second.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Exactly(2));
    [Fact] void should_not_complete() => _job.AllStepsCompletedCount.ShouldEqual(0);
}
