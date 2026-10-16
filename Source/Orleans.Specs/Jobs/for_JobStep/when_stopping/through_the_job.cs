// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class through_the_job : given.a_job_step_left_scheduled_by_a_dead_process
{
    bool _returnedBeforeTheWorkSettled;
    bool _returnedBeforeTheStopWasDelivered;
    bool _cancelledWhenReturned;

    async Task Establish()
    {
        _jobStep.HoldPerformOpen = true;
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _jobStep.Performed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        await _jobStep.OnJobStopped().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _returnedBeforeTheWorkSettled = !_jobStep.WorkCompleted.Task.IsCompleted;
        _returnedBeforeTheStopWasDelivered = !_jobStep.OutcomeDelivery.IsCompleted;
        _cancelledWhenReturned = _jobStep.PerformToken.IsCancellationRequested;

        _jobStep.Gate.TrySetResult();
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_return_before_the_work_settles() => _returnedBeforeTheWorkSettled.ShouldBeTrue();
    [Fact] void should_return_before_the_stop_is_delivered() => _returnedBeforeTheStopWasDelivered.ShouldBeTrue();
    [Fact] void should_have_requested_cancellation_when_returning() => _cancelledWhenReturned.ShouldBeTrue();
    [Fact] async Task should_report_the_stop_once_the_work_settles() => await _jobStep.ReportedJob.Received(1).OnStepStopped(_jobStepId, Arg.Any<JobStepResult>());
    [Fact] void should_record_the_step_as_stopped() => _persistedState.Status.ShouldEqual(JobStepStatus.Stopped);

    void Destroy()
    {
        _jobStep.Gate.TrySetResult();
        _jobStep.Dispose();
    }
}
