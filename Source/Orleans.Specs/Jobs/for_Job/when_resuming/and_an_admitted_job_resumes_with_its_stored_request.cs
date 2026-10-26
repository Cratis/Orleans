// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_an_admitted_job_resumes_with_its_stored_request : given.a_running_job_with_two_steps
{
    Result<ResumeJobSuccess, ResumeJobError> _result;
    given.SomeAdmission _admission;
    IJobRequest _request;
    int _plansBefore;
    int _resumeHooksBefore;
    int _dispatchesBefore;

    void Establish()
    {
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Stopped });
        _job.CurrentState.Status = JobStatus.Stopped;
        _job.ShouldBeResumable = true;
        _request = _job.CurrentState.Request;
        _admission = given.SomeAdmission.Admitting();
        Admit(_admission);
        _plansBefore = _job.PrepareStepsCount;
        _resumeHooksBefore = _job.BeforeResumingCount;
        _dispatchesBefore = _job.DispatchContexts.Count;
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_resume_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_ask_once() => _admission.Asked.Count.ShouldEqual(1);
    [Fact] void should_ask_about_the_stored_request_and_job_identity() => _admission.Asked.Single().ShouldEqual(new JobAdmissionRequest(_jobKey, _jobId, _job.CurrentState.Type, _request));
    [Fact] void should_pass_the_stored_request_instance() => _admission.Asked.Single().Request.ShouldBeSame(_request);
    [Fact] void should_be_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_run_the_resume_hook_once() => _job.BeforeResumingCount.ShouldEqual(_resumeHooksBefore + 1);
    [Fact] void should_dispatch_once() => _job.DispatchContexts.Count.ShouldEqual(_dispatchesBefore + 1);
    [Fact] void should_restart_the_first_step_once() => _first.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Exactly(2));
    [Fact] void should_restart_the_second_step_once() => _second.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Exactly(2));
    [Fact] void should_not_plan_again() => _job.PrepareStepsCount.ShouldEqual(_plansBefore);
}
