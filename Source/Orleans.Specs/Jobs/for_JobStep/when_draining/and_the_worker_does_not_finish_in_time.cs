// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobStep.when_draining;

public class and_the_worker_does_not_finish_in_time : given.a_job_step_left_scheduled_by_a_dead_process
{
    JobStepDrainResult _result;
    bool _stopRecordedBeforeTheWorkSettled;

    async Task Establish()
    {
        _jobStep.HoldPerformOpen = true;
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _jobStep.Performed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        _result = await _jobStep.Drain(1, TimeSpan.FromMilliseconds(50)).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _stopRecordedBeforeTheWorkSettled = _storage.Persisted.Any(_ => _.Status == JobStepStatus.Stopped);
        _jobStep.Gate.TrySetResult();
        await _jobStep.WorkCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_not_be_drained() => _result.Drained.ShouldBeFalse();
    [Fact] void should_not_record_the_stop_before_the_work_settles() => _stopRecordedBeforeTheWorkSettled.ShouldBeFalse();
    [Fact] async Task should_report_the_stop_once_the_work_settles() => await _jobStep.ReportedJob.Received(1).OnStepStopped(_jobStepId, Arg.Any<JobStepResult>());
    [Fact] void should_record_the_step_as_stopped_once() => _storage.Persisted.Count(_ => _.Status == JobStepStatus.Stopped).ShouldEqual(1);

    void Destroy()
    {
        _jobStep.Gate.TrySetResult();
        _jobStep.Dispose();
    }
}
