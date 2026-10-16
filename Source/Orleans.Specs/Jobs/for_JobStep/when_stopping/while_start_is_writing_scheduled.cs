// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class while_start_is_writing_scheduled : given.a_job_step_left_scheduled_by_a_dead_process
{
    readonly TaskCompletionSource _scheduledWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task<Result<StartJobStepError>> _starting;
    Result<JobStepError> _stopped;

    async Task Establish()
    {
        _storage.HoldWrites = _scheduledWrite;
        _starting = _jobStep.Start(GrainId.Create("job", _jobId.ToString()));
        await _storage.WriteHeld.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        var stopping = _jobStep.Stop(false);
        _storage.HoldWrites = null;
        _scheduledWrite.TrySetResult();
        await _starting.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _stopped = await stopping.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_succeed() => _stopped.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_launch_the_work() => _jobStep.WorkEntered.Task.IsCompleted.ShouldBeFalse();
    [Fact] void should_record_the_step_as_stopped() => _persistedState.Status.ShouldEqual(JobStepStatus.Stopped);
    [Fact] async Task should_report_the_stop_to_the_job_once() => await _jobStep.ReportedJob.Received(1).OnStepStopped(_jobStepId, Arg.Any<JobStepResult>());

    void Destroy()
    {
        _scheduledWrite.TrySetResult();
        _jobStep.Dispose();
    }
}
