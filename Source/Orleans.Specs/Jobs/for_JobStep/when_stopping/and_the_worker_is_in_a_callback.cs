// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class and_the_worker_is_in_a_callback : given.a_job_step_left_scheduled_by_a_dead_process
{
    readonly TaskCompletionSource _inCallback = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _finishCallback = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _stoppedWhileInTheCallback;
    Result<JobStepError> _stopped;

    async Task Establish()
    {
        _jobStep.Work = async (step, _) => await step.OnActivation(async () =>
        {
            _inCallback.TrySetResult();
            await _finishCallback.Task;
            await step.Checkpoint("in callback");
        });
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _inCallback.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        var stopping = _jobStep.Stop(false);
        await _jobStep.PerformCancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _stoppedWhileInTheCallback = stopping.IsCompleted;
        _finishCallback.TrySetResult();
        _stopped = await stopping.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_not_finish_stopping_while_the_callback_runs() => _stoppedWhileInTheCallback.ShouldBeFalse();
    [Fact] void should_succeed() => _stopped.IsSuccess.ShouldBeTrue();
    [Fact] void should_persist_the_checkpoint_before_the_stop() => _storage.Persisted.Select(_ => _.Message).ToList().IndexOf("in callback").ShouldBeLessThan(_storage.Persisted.Select(_ => _.Status).ToList().IndexOf(JobStepStatus.Stopped));
    [Fact] void should_record_the_step_as_stopped() => _persistedState.Status.ShouldEqual(JobStepStatus.Stopped);
    [Fact] async Task should_report_the_stop_to_the_job_once() => await _jobStep.ReportedJob.Received(1).OnStepStopped(_jobStepId, Arg.Any<JobStepResult>());

    void Destroy()
    {
        _finishCallback.TrySetResult();
        _jobStep.Dispose();
    }
}
