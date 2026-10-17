// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_admission_refuses_a_stopped_job : given.a_running_job_with_two_steps
{
    Result<ResumeJobSuccess, ResumeJobError> _result;
    int _writesBefore;

    void Establish()
    {
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Stopped });
        _job.CurrentState.Status = JobStatus.Stopped;
        _job.ShouldBeResumable = true;
        Admit(given.SomeAdmission.Refusing());
        _writesBefore = Writes;
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_report_not_admitted() => ((CannotResumeJobError)_result.AsT1).ShouldEqual(CannotResumeJobError.NotAdmitted);
    [Fact] void should_stay_stopped() => _job.CurrentState.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_write_nothing() => Writes.ShouldEqual(_writesBefore);
    [Fact] void should_not_start_any_step_again() => _first.Verify(_ => _.Start(It.IsAny<GrainId>()), Times.Once);
}
