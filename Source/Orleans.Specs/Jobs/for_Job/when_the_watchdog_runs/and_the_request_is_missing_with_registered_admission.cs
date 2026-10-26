// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_the_watchdog_runs;

public class and_the_request_is_missing_with_registered_admission : for_Job.given.an_interrupted_job_rejecting_resume
{
    for_Job.given.SomeAdmission _admission;
    for_Job.given.SomeRequest _request;
    int _writesBefore;
    int _writeAttemptsBefore;
    int _resumeHooksBefore;
    int _startHooksBefore;
    int _dispatchesBefore;
    int _plansBefore;
    int _writesWhileMissing;
    int _writeAttemptsWhileMissing;
    int _resumeHooksWhileMissing;
    int _startHooksWhileMissing;
    int _dispatchesWhileMissing;
    int _startsWhileMissing;
    int _completedWhileMissing;
    int _asksWhileMissing;
    JobStatus _statusWhileMissing;

    async Task Establish()
    {
        CountStepsInStorage((JobStepStatus.Running, 1));
        _ = await _job.Resume();
        _request = (for_Job.given.SomeRequest)_job.CurrentState.Request;
        _job.CurrentState.Request = null!;
        _admission = for_Job.given.SomeAdmission.Refusing();
        Admit(_admission);
        _job.BeforeResuming = () => Task.CompletedTask;
        _writesBefore = Writes;
        _writeAttemptsBefore = _job.WriteAttempts;
        _resumeHooksBefore = _job.BeforeResumingCount;
        _startHooksBefore = _job.BeforeStartingCount;
        _dispatchesBefore = _job.DispatchContexts.Count;
        _plansBefore = _job.PrepareStepsCount;
    }

    async Task Because()
    {
        await _job.RunWatchdog();
        await _job.RunWatchdog();
        _writesWhileMissing = Writes;
        _writeAttemptsWhileMissing = _job.WriteAttempts;
        _resumeHooksWhileMissing = _job.BeforeResumingCount;
        _startHooksWhileMissing = _job.BeforeStartingCount;
        _dispatchesWhileMissing = _job.DispatchContexts.Count;
        _startsWhileMissing = _unfinished.Invocations.Count(call => call.Method.Name == nameof(for_Job.given.ISomeJobStep.Start));
        _completedWhileMissing = _job.AllStepsCompletedCount;
        _asksWhileMissing = _admission.Asked.Count;
        _statusWhileMissing = _job.CurrentState.Status;

        _job.CurrentState.Request = _request;

        // The capped backoff skips at most fifteen ticks; stop as soon as the real request is evaluated.
        for (var tick = 0; tick < 16 && _admission.Asked.Count == 0; tick++)
        {
            await _job.RunWatchdog();
        }
    }

    [Fact] void should_have_an_owed_resume_hook() => _resumeHooksBefore.ShouldEqual(1);
    [Fact] void should_not_write_while_the_request_is_missing() => _writesWhileMissing.ShouldEqual(_writesBefore);
    [Fact] void should_not_attempt_a_write_while_the_request_is_missing() => _writeAttemptsWhileMissing.ShouldEqual(_writeAttemptsBefore);
    [Fact] void should_not_retry_the_hook_without_a_request() => _resumeHooksWhileMissing.ShouldEqual(_resumeHooksBefore);
    [Fact] void should_not_run_the_start_hook_without_a_request() => _startHooksWhileMissing.ShouldEqual(_startHooksBefore);
    [Fact] void should_not_dispatch_without_a_request() => _dispatchesWhileMissing.ShouldEqual(_dispatchesBefore);
    [Fact] void should_not_start_a_step_without_a_request() => _startsWhileMissing.ShouldEqual(0);
    [Fact] void should_not_complete_without_a_request() => _completedWhileMissing.ShouldEqual(0);
    [Fact] void should_remain_running_while_the_request_is_missing() => _statusWhileMissing.ShouldEqual(JobStatus.Running);
    [Fact] void should_not_ask_without_a_real_request() => _asksWhileMissing.ShouldEqual(0);
    [Fact] void should_ask_once_after_the_request_is_restored() => _admission.Asked.Count.ShouldEqual(1);
    [Fact] void should_pass_the_restored_request_instance() => _admission.Asked.Single().Request.ShouldBeSame(_request);
    [Fact] void should_honor_the_real_refusal() => _job.CurrentState.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_not_run_the_resume_hook_after_refusal() => _job.BeforeResumingCount.ShouldEqual(_resumeHooksBefore);
    [Fact] void should_not_dispatch_after_refusal() => _job.DispatchContexts.Count.ShouldEqual(_dispatchesBefore);
    [Fact] void should_not_plan_again() => _job.PrepareStepsCount.ShouldEqual(_plansBefore);
    [Fact] void should_not_start_the_unfinished_step() => _unfinished.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_not_start_the_stopped_step() => _stopped.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
}
