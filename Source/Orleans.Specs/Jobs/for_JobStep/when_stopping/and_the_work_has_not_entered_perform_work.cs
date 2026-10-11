// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workers;

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class and_the_work_has_not_entered_perform_work : given.a_job_step_left_scheduled_by_a_dead_process
{
    PerformWorkResult<JobStepResult> _result;

    async Task Establish()
    {
        _jobStep.HoldWorkBeforePerformWork = true;
        _jobStep.CancelledResult = "checkpoint";
        var started = await _jobStep.Start(GrainId.Create("job", _jobId.ToString()));
        started.IsSuccess.ShouldBeTrue();
        await _jobStep.WorkEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        var stopped = await _jobStep.Stop(false);
        stopped.IsSuccess.ShouldBeTrue();
        _jobStep.WorkGate.TrySetResult();
        _result = await _jobStep.WorkCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        (await _jobStep.Stop(false)).IsSuccess.ShouldBeTrue();
    }

    [Fact] void should_cancel_the_work() => _result.Error.ShouldEqual(PerformWorkError.Cancelled);
    [Fact] void should_not_perform_the_step() => _jobStep.Performed.Task.IsCompleted.ShouldBeFalse();
    [Fact] void should_report_the_step_as_stopped() => _persistedState.Status.ShouldEqual(JobStepStatus.Stopped);
    [Fact] async Task should_report_the_stop_to_the_job_once_even_after_another_stop() => await _jobStep.ReportedJob.Received(1).OnStepStopped(_jobStepId, Arg.Any<JobStepResult>());
    [Fact] async Task should_report_the_partial_result_from_the_current_state() => await _jobStep.ReportedJob.Received(1).OnStepStopped(_jobStepId, Arg.Is<JobStepResult>(result => HasCancelledPartialResult(result)));
    [Fact] async Task should_not_report_failure_to_the_job() => await _jobStep.ReportedJob.DidNotReceive().OnStepFailed(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>());

    void Destroy()
    {
        _jobStep.WorkGate.TrySetResult();
        _jobStep.Dispose();
    }

    static bool HasCancelledPartialResult(JobStepResult result) => result.TryGetError(out var error) && error.Cancelled && Equals(error.PartialResult, "checkpoint");
}
