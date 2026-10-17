// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class and_the_step_is_stopped_again_after_building_the_cancelled_result_faulted : given.a_job_step_left_scheduled_by_a_dead_process
{
    async Task Establish()
    {
        _jobStep.HoldWorkBeforePerformWork = true;
        _jobStep.CancelledResult = "checkpoint";
        _jobStep.HoldCancelledResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _jobStep.WorkEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        var stopping = _jobStep.Stop(false);
        _jobStep.WorkGate.TrySetResult();
        await _jobStep.CancelledResultRequested.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _jobStep.HoldCancelledResult.TrySetException(new given.CancelledResultUnavailable());
        (await stopping.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System)).IsSuccess.ShouldBeTrue();
        await _jobStep.WorkCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        (await _jobStep.Stop(false).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System)).IsSuccess.ShouldBeTrue();
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_keep_the_persisted_step_stopped() => _storage.Persisted[^1].Status.ShouldEqual(JobStepStatus.Stopped);
    [Fact] void should_not_perform_the_step() => _jobStep.Performed.Task.IsCompleted.ShouldBeFalse();
    [Fact] async Task should_not_report_the_stop_a_second_time() => await _jobStep.ReportedJob.Received(1).OnStepStopped(_jobStepId, Arg.Any<JobStepResult>());
    [Fact] async Task should_keep_the_cancelled_with_no_result_fallback() => await _jobStep.ReportedJob.Received(1).OnStepStopped(_jobStepId, Arg.Is<JobStepResult>(result => IsTheCancelledWithNoResultFallback(result)));
    [Fact] async Task should_not_report_failure() => await _jobStep.ReportedJob.DidNotReceive().OnStepFailed(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>());
    [Fact] async Task should_not_report_success() => await _jobStep.ReportedJob.DidNotReceive().OnStepSucceeded(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>());

    void Destroy()
    {
        _jobStep.WorkGate.TrySetResult();
        _jobStep.HoldCancelledResult!.TrySetResult();
        _jobStep.Dispose();
    }

    static bool IsTheCancelledWithNoResultFallback(JobStepResult result)
    {
        // Pin the current fallback representation; update this expectation with Cratis/Orleans#93.
        var expected = PerformJobStepError.CancelledWithNoResult();
        return result.TryGetError(out var error) && error.Cancelled && Equals(error.PartialResult, expected.PartialResult) && error.ErrorMessages.SequenceEqual(expected.ErrorMessages);
    }
}
