// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_admission_throws : given.a_job_left_running
{
    int _writesBefore;

    void Establish()
    {
        _job.CurrentState.Request = new given.SomeRequest();
        Admit(given.SomeAdmission.Throwing());
        _writesBefore = Writes;
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_report_unknown_rather_than_not_admitted() => ((CannotResumeJobError)_result.AsT1).ShouldEqual(CannotResumeJobError.Unknown);
    [Fact] void should_stay_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_not_have_a_drain_pending() => _job.CurrentState.DrainPending.ShouldBeFalse();
    [Fact] void should_write_nothing() => Writes.ShouldEqual(_writesBefore);
    [Fact] void should_not_start_any_step() => _unfinished.Verify(_ => _.Start(It.IsAny<GrainId>()), Times.Never);
}
