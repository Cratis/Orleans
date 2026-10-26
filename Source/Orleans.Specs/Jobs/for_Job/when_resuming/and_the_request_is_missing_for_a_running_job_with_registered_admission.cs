// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_request_is_missing_for_a_running_job_with_registered_admission : given.a_job_left_running
{
    given.SomeAdmission _admission;
    int _writesBefore;
    int _writeAttemptsBefore;
    int _resumeHooksBefore;
    int _startHooksBefore;
    int _dispatchesBefore;
    int _plansBefore;
    JobStatus[] _historyBefore;
    (int Total, int Successful, int Stopped) _progressBefore;

    void Establish()
    {
        _job.CurrentState.StatusChanges.Insert(0, new() { Status = JobStatus.PreparingJob });
        _job.ShouldBeResumable = true;
        _job.CurrentState.Request = null!;
        _admission = given.SomeAdmission.Refusing();
        Admit(_admission);
        _writesBefore = Writes;
        _writeAttemptsBefore = _job.WriteAttempts;
        _resumeHooksBefore = _job.BeforeResumingCount;
        _startHooksBefore = _job.BeforeStartingCount;
        _dispatchesBefore = _job.DispatchContexts.Count;
        _plansBefore = _job.PrepareStepsCount;
        _historyBefore = _job.CurrentState.StatusChanges.Select(change => change.Status).ToArray();
        _progressBefore = (_job.CurrentState.Progress.TotalSteps, _job.CurrentState.Progress.SuccessfulSteps, _job.CurrentState.Progress.StoppedSteps);
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_fail_to_resume() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_unknown_instead_of_an_owner_refusal() => ((CannotResumeJobError)_result.AsT1).ShouldEqual(CannotResumeJobError.Unknown);
    [Fact] void should_not_ask_a_policy_without_a_real_request() => _admission.Asked.ShouldBeEmpty();
    [Fact] void should_stay_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_not_leave_a_drain_pending() => _job.CurrentState.DrainPending.ShouldBeFalse();
    [Fact] void should_not_add_status_history() => _job.CurrentState.StatusChanges.Select(change => change.Status).ShouldEqual(_historyBefore);
    [Fact] void should_preserve_progress() => (_job.CurrentState.Progress.TotalSteps, _job.CurrentState.Progress.SuccessfulSteps, _job.CurrentState.Progress.StoppedSteps).ShouldEqual(_progressBefore);
    [Fact] void should_write_nothing() => Writes.ShouldEqual(_writesBefore);
    [Fact] void should_not_attempt_a_write() => _job.WriteAttempts.ShouldEqual(_writeAttemptsBefore);
    [Fact] void should_not_run_the_resume_hook() => _job.BeforeResumingCount.ShouldEqual(_resumeHooksBefore);
    [Fact] void should_not_run_the_start_hook() => _job.BeforeStartingCount.ShouldEqual(_startHooksBefore);
    [Fact] void should_not_dispatch() => _job.DispatchContexts.Count.ShouldEqual(_dispatchesBefore);
    [Fact] void should_not_plan_again() => _job.PrepareStepsCount.ShouldEqual(_plansBefore);
    [Fact] void should_not_restart_the_unfinished_step() => _unfinished.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_not_restart_the_completed_step() => _completed.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_not_restart_the_stopped_step() => _stopped.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_not_drain_the_unfinished_step() => _unfinished.Verify(step => step.Drain(It.IsAny<long>(), It.IsAny<TimeSpan>()), Times.Never);
    [Fact] void should_not_drain_the_stopped_step() => _stopped.Verify(step => step.Drain(It.IsAny<long>(), It.IsAny<TimeSpan>()), Times.Never);
    [Fact] void should_not_complete() => _job.AllStepsCompletedCount.ShouldEqual(0);
    [Fact] void should_not_run_the_stopped_hook() => _job.StoppedCount.ShouldEqual(0);
}
