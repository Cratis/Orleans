// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class and_the_job_is_removed_while_its_stop_is_pending : given.a_job_step_with_a_gated_throttle
{
    readonly TaskCompletionSource _claim = new(TaskCreationOptions.RunContinuationsAsynchronously);

    async Task Establish()
    {
        _jobStep.HoldCancelledResult = _claim;
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _throttle.Waiting.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        await _jobStep.OnJobStopped();
        await _jobStep.CancelledResultRequested.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OnJobRemoved();
        _claim.TrySetResult();
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] async Task should_report_the_stop_once() => await _jobStep.ReportedJob.Received(1).OnStepStopped(_jobStepId, Arg.Any<JobStepResult>());
    [Fact] void should_record_the_stop_once() => _storage.Persisted.Count(_ => _.Status == JobStepStatus.Stopped).ShouldEqual(1);
    [Fact] void should_remove_the_step_once() => _storage.Persisted.Count(_ => _.Status == JobStepStatus.Removing).ShouldEqual(1);
    [Fact] void should_remove_the_step_after_recording_the_stop() => _storage.Persisted[^1].Status.ShouldEqual(JobStepStatus.Removing);
}
