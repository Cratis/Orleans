// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_starting;

public class after_the_work_settled_but_before_the_stop_claimed_the_outcome : given.a_job_step_with_a_gated_throttle
{
    readonly TaskCompletionSource _claim = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Result<StartJobStepError> _result;

    async Task Establish()
    {
        // The stop is held while it claims the outcome, after the cancelled work has already returned.
        _jobStep.HoldCancelledResult = _claim;
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _throttle.Waiting.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OnJobStopped();
        await _jobStep.WorkCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.CancelledResultRequested.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        _result = await _jobStep.Start(GrainId.Create("job", _jobId.ToString())).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _claim.TrySetResult();
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_refuse_as_already_started() => ((StartJobStepError)_result).ShouldEqual(StartJobStepError.AlreadyStarted);
    [Fact] void should_not_launch_new_work() => _jobStep.WorkRuns.ShouldEqual(1);
    [Fact] async Task should_report_the_stop_once() => await _jobStep.ReportedJob.Received(1).OnStepStopped(_jobStepId, Arg.Any<JobStepResult>());
    [Fact] void should_record_the_step_as_stopped() => _persistedState.Status.ShouldEqual(JobStepStatus.Stopped);
}
