// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_invoking_on_activation;

public class concurrently_with_a_status_write : given.a_job_step_left_scheduled_by_a_dead_process
{
    readonly TaskCompletionSource _inCallback = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseCallback = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _statusChangedDuringTheCallback;
    Result<JobStepError> _written;

    async Task Establish()
    {
        _jobStep.Work = async (step, _) => await step.OnActivation(async () =>
        {
            var statusChanges = step.LiveState.StatusChanges.Count;
            _inCallback.TrySetResult();
            await _releaseCallback.Task;
            _statusChangedDuringTheCallback = step.LiveState.StatusChanges.Count != statusChanges;
            await step.Checkpoint("callback");
        });
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _inCallback.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        var writing = _jobStep.ReportStatusChange(JobStepStatus.Running);
        _releaseCallback.TrySetResult();
        _written = await writing.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.WorkCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_not_change_the_status_while_the_callback_runs() => _statusChangedDuringTheCallback.ShouldBeFalse();
    [Fact] void should_write_the_status_once_the_callback_is_done() => _written.IsSuccess.ShouldBeTrue();

    void Destroy()
    {
        _releaseCallback.TrySetResult();
        _jobStep.Dispose();
    }
}
