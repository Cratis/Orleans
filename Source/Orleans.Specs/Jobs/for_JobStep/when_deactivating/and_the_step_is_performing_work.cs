// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_deactivating;

public class and_the_step_is_performing_work : given.a_job_step_left_scheduled_by_a_dead_process
{
    readonly TaskCompletionSource _finishWork = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task _deactivation;
    bool _deactivatedBeforeTheWorkerSettled;
    Result<ActivationCallbackRejected> _finalCheckpoint;

    async Task Establish()
    {
        _jobStep.Work = async (step, _) =>
        {
            // Work that ignores cancellation until released, then checkpoints on the way out.
            await _finishWork.Task;
            _finalCheckpoint = await step.OnActivation(() => (Task)step.Checkpoint("final"));
        };
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _jobStep.Performed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        _deactivation = _jobStep.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.ShuttingDown, string.Empty), CancellationToken.None);
        await _jobStep.PerformCancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _deactivatedBeforeTheWorkerSettled = _deactivation.IsCompleted;
        _finishWork.TrySetResult();
        await _deactivation.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.WorkCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_not_finish_deactivating_before_the_worker_settles() => _deactivatedBeforeTheWorkerSettled.ShouldBeFalse();
    [Fact] void should_admit_the_final_checkpoint() => _finalCheckpoint.IsSuccess.ShouldBeTrue();
    [Fact] void should_persist_the_final_checkpoint() => _storage.Persisted[^1].ShouldEqual(new given.PersistedStep(JobStepStatus.Running, "final"));
    [Fact] void should_leave_the_step_running_for_recovery() => _persistedState.Status.ShouldEqual(JobStepStatus.Running);
    [Fact] async Task should_not_report_a_stop_to_the_job() => await _jobStep.ReportedJob.DidNotReceive().OnStepStopped(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>());
    [Fact] async Task should_not_report_failure_to_the_job() => await _jobStep.ReportedJob.DidNotReceive().OnStepFailed(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>());

    void Destroy()
    {
        _finishWork.TrySetResult();
        _jobStep.Dispose();
    }
}
