// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobStep.when_deactivating;

public class and_the_worker_does_not_finish_in_time : given.a_job_step_left_scheduled_by_a_dead_process
{
    bool _deactivatedWhileTheWorkerRan;
    int _writesWhenDeactivated;

    async Task Establish()
    {
        _jobStep.HoldPerformOpen = true;
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _jobStep.Performed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        // Orleans' deactivation deadline has already passed.
        using var expired = new CancellationTokenSource();
        await expired.CancelAsync();
        await _jobStep.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.ShuttingDown, string.Empty), expired.Token).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _deactivatedWhileTheWorkerRan = !_jobStep.WorkCompleted.Task.IsCompleted;
        _writesWhenDeactivated = _storage.Persisted.Count;

        _jobStep.Gate.TrySetResult();
        await _jobStep.WorkCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_finish_deactivating_without_waiting_for_the_worker() => _deactivatedWhileTheWorkerRan.ShouldBeTrue();
    [Fact] void should_not_write_after_deactivating() => _storage.Persisted.Count.ShouldEqual(_writesWhenDeactivated);
    [Fact] void should_leave_the_step_running_for_recovery() => _persistedState.Status.ShouldEqual(JobStepStatus.Running);
    [Fact] async Task should_not_report_a_stop_to_the_job() => await _jobStep.ReportedJob.DidNotReceive().OnStepStopped(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>());

    void Destroy()
    {
        _jobStep.Gate.TrySetResult();
        _jobStep.Dispose();
    }
}
