// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_running_in_stages;

public class and_the_request_is_missing_with_registered_admission : given.a_job_with_three_stages
{
    for_Job.given.SomeAdmission _admission;
    for_Job.given.SomeRequest _request;
    JobStatus _statusWhileMissing;
    int _completedWhileMissing;
    int _stoppedHooksWhileMissing;
    int _asksWhileMissing;
    int _dispatchesBefore;
    int _dispatchesWhileMissing;

    async Task Establish()
    {
        _admission = for_Job.given.SomeAdmission.Admitting();
        Admit(_admission);
        await _job.Start(new());
        _request = (for_Job.given.SomeRequest)_job.CurrentState.Request;
        _admission.Answer = () => Task.FromResult(false);
        _admission.Asked.Clear();
        _job.CurrentState.Request = null!;
        _dispatchesBefore = _job.DispatchContexts.Count;
    }

    async Task Because()
    {
        await Succeed(_first);
        ShouldNotHaveStarted(_secondStepA);
        ShouldNotHaveStarted(_secondStepB);
        await _silo.TimerRegistry.FireAllAsync();
        _statusWhileMissing = _job.CurrentState.Status;
        _completedWhileMissing = _job.AllStepsCompletedCount;
        _stoppedHooksWhileMissing = _job.StoppedCount;
        _asksWhileMissing = _admission.Asked.Count;
        _dispatchesWhileMissing = _job.DispatchContexts.Count;
        ShouldNotHaveStarted(_secondStepA);
        ShouldNotHaveStarted(_secondStepB);

        _job.CurrentState.Request = _request;
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_stay_running_while_the_request_is_missing() => _statusWhileMissing.ShouldEqual(JobStatus.Running);
    [Fact] void should_not_complete_while_the_request_is_missing() => _completedWhileMissing.ShouldEqual(0);
    [Fact] void should_not_run_the_stopped_hook_while_the_request_is_missing() => _stoppedHooksWhileMissing.ShouldEqual(0);
    [Fact] void should_not_ask_without_a_real_request() => _asksWhileMissing.ShouldEqual(0);
    [Fact] void should_not_dispatch_while_the_request_is_missing() => _dispatchesWhileMissing.ShouldEqual(_dispatchesBefore);
    [Fact] void should_ask_once_after_the_request_is_restored() => _admission.Asked.Count.ShouldEqual(1);
    [Fact] void should_pass_the_restored_request_instance() => _admission.Asked.Single().Request.ShouldBeSame(_request);
    [Fact] void should_stop_after_the_real_refusal() => _job.CurrentState.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_not_start_the_first_step_in_the_next_stage() => ShouldNotHaveStarted(_secondStepA);
    [Fact] void should_not_start_the_second_step_in_the_next_stage() => ShouldNotHaveStarted(_secondStepB);
    [Fact] void should_not_start_the_last_stage() => ShouldNotHaveStarted(_thirdStep);
}
