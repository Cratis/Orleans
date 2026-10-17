// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_a_drain_is_pending : given.a_running_job_with_two_steps
{
    Result<ResumeJobSuccess, ResumeJobError> _result;
    given.SomeAdmission _admission;

    async Task Establish()
    {
        DrainsAs(_first, _firstId, JobStepStatus.Stopped, Stopped);
        DoesNotDrain(_second);
        (await _job.StopAndDrain(TimeSpan.Zero)).ShouldEqual(JobDrainStatus.TimedOut);
        _job.ShouldBeResumable = true;
        _admission = given.SomeAdmission.Refusing();
        Admit(_admission);
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_refuse() => ((CannotResumeJobError)_result.AsT1).ShouldEqual(CannotResumeJobError.JobCannotBeResumed);
    [Fact] void should_stay_stopped() => _job.CurrentState.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_not_ask_admission() => _admission.Asked.Count.ShouldEqual(0);
    [Fact] void should_not_start_any_step_again() => _second.Verify(_ => _.Start(It.IsAny<GrainId>()), Times.Once);
}
