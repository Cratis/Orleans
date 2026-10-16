// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_JobStep.when_draining;

public class with_a_zero_timeout : given.a_job_step_left_scheduled_by_a_dead_process
{
    readonly TaskCompletionSource _polled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<bool> _finalCheckpoint = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _finalCheckpointAdmitted;
    JobStepDrainResult _poll;
    JobStepDrainResult _drained;

    async Task Establish()
    {
        // The work writes a final checkpoint once it sees the cancellation, but only after the job has polled.
        _jobStep.Work = async (step, _) =>
        {
            await step.PerformCancelled.Task;
            await _polled.Task;
            _finalCheckpoint.TrySetResult((await step.OnActivation(() => step.Checkpoint("final"))).IsSuccess);
        };
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _jobStep.Performed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        _poll = await _jobStep.Drain(1, TimeSpan.Zero).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _polled.TrySetResult();
        _finalCheckpointAdmitted = await _finalCheckpoint.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _drained = await _jobStep.Drain(1, TimeSpan.FromSeconds(5)).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_not_be_drained_by_the_poll() => _poll.Drained.ShouldBeFalse();
    [Fact] void should_admit_the_final_checkpoint() => _finalCheckpointAdmitted.ShouldBeTrue();
    [Fact] void should_be_drained_once_the_work_settled() => _drained.Drained.ShouldBeTrue();
    [Fact] void should_persist_the_final_checkpoint_before_the_stop() => _storage.Persisted.Select(_ => _.Message).ToList().IndexOf("final").ShouldBeLessThan(_storage.Persisted.Select(_ => _.Status).ToList().IndexOf(JobStepStatus.Stopped));

    async Task Destroy()
    {
        _polled.TrySetResult();
        await _jobStep.OutcomeDelivery;
        _jobStep.Dispose();
    }
}
