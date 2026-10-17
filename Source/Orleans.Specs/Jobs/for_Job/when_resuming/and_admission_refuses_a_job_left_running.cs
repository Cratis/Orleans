// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_admission_refuses_a_job_left_running : given.a_job_left_running
{
    int _writesBefore;
    int _writesDuringTheTurn;
    int _drainsDuringTheTurn;

    void Establish()
    {
        _job.CurrentState.Request = new given.SomeRequest();
        _job.CurrentState.StatusChanges.Insert(0, new() { Status = JobStatus.PreparingJob });
        _job.ShouldBeResumable = true;
        DrainsAs(_unfinished, _unfinishedId, JobStepStatus.Stopped, Stopped);
        DrainsAs(_stopped, _stoppedId, JobStepStatus.Stopped, null);
        Admit(given.SomeAdmission.Refusing());
        _writesBefore = Writes;
    }

    async Task Because()
    {
        _result = await _job.Resume();
        _writesDuringTheTurn = Writes - _writesBefore;
        _drainsDuringTheTurn = _unfinished.Invocations.Count(_ => _.Method.Name == nameof(given.ISomeJobStep.Drain));
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_report_not_admitted() => ((CannotResumeJobError)_result.AsT1).ShouldEqual(CannotResumeJobError.NotAdmitted);
    [Fact] void should_write_the_stop_and_the_pending_drain_once_in_the_turn() => _writesDuringTheTurn.ShouldEqual(1);
    [Fact] void should_not_drain_any_step_in_the_turn() => _drainsDuringTheTurn.ShouldEqual(0);
    [Fact] void should_drain_the_step_once_the_drain_turn_runs() => _unfinished.Verify(_ => _.Drain(It.IsAny<long>(), It.IsAny<TimeSpan>()), Times.Once);
    [Fact] void should_not_start_any_step() => _unfinished.Verify(_ => _.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_be_stopped_once_drained() => _job.CurrentState.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_no_longer_have_a_drain_pending() => _job.CurrentState.DrainPending.ShouldBeFalse();
    [Fact] void should_run_the_stopped_hook_once() => _job.StoppedCount.ShouldEqual(1);
}
