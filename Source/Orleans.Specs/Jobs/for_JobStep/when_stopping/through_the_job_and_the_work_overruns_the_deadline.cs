// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class through_the_job_and_the_work_overruns_the_deadline : given.a_job_step_left_scheduled_by_a_dead_process
{
    readonly TaskCompletionSource _deadlinePassed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _stopRecordedBeforeTheWorkSettled;

    protected override TimeSpan StopTimeout => TimeSpan.FromMilliseconds(100);

    async Task Establish()
    {
        // The work ignores cancellation until released.
        _jobStep.HoldPerformOpen = true;
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _jobStep.Performed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        await _jobStep.OnJobStopped();

        // The stop closes admission to the run's callbacks once its deadline has passed without the work settling.
        _jobStep.CurrentRun!.AdmissionClosed.Register(() => _deadlinePassed.TrySetResult());
        await _deadlinePassed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _stopRecordedBeforeTheWorkSettled = _storage.Persisted.Any(_ => _.Status == JobStepStatus.Stopped);

        _jobStep.Gate.TrySetResult();
        await _jobStep.WorkCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_not_record_the_stop_before_the_work_settles() => _stopRecordedBeforeTheWorkSettled.ShouldBeFalse();
    [Fact] async Task should_report_the_stop_once_the_work_settles() => await _jobStep.ReportedJob.Received(1).OnStepStopped(_jobStepId, Arg.Any<JobStepResult>());
    [Fact] void should_record_the_step_as_stopped_once() => _storage.Persisted.Count(_ => _.Status == JobStepStatus.Stopped).ShouldEqual(1);
    [Fact] void should_end_stopped() => _persistedState.Status.ShouldEqual(JobStepStatus.Stopped);

    void Destroy()
    {
        _jobStep.Gate.TrySetResult();
        _jobStep.Dispose();
    }
}
