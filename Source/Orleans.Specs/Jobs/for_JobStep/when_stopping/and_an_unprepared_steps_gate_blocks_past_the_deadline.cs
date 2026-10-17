// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class and_an_unprepared_steps_gate_blocks_past_the_deadline : given.a_job_step_left_scheduled_by_a_dead_process
{
    readonly TaskCompletionSource _write = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task<Result<JobStepError>> _writing;
    Result<JobStepError> _stopped;

    protected override TimeSpan StopTimeout => TimeSpan.FromMilliseconds(100);

    async Task Establish()
    {
        _persistedState.IsPrepared = false;
        _storage.HoldWrites = _write;
        _writing = _jobStep.ReportStatusChange(JobStepStatus.Scheduled);
        await _storage.WriteHeld.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        _stopped = await _jobStep.Stop(false).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _write.TrySetResult();
        (await _writing.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System)).IsSuccess.ShouldBeTrue();
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_keep_the_stop_timed_out_result() => _stopped.AsT1.ShouldEqual(JobStepError.StopTimedOut);
    [Fact] void should_not_record_an_unprepared_step_as_stopped() => _storage.Persisted.Any(step => step.Status is JobStepStatus.Stopped).ShouldBeFalse();
    [Fact] void should_release_the_stop_reservation_after_the_gate_opens() => _jobStep.CurrentRun!.IsSettled.ShouldBeTrue();
    [Fact] void should_not_attempt_parent_resolution() => _jobStep.ResolvedJobIds.ShouldBeEmpty();

    void Destroy()
    {
        _write.TrySetResult();
        _jobStep.Dispose();
    }
}
