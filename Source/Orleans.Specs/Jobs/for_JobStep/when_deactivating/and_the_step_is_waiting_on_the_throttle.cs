// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workers;

namespace Cratis.Orleans.Jobs.for_JobStep.when_deactivating;

public class and_the_step_is_waiting_on_the_throttle : given.a_job_step_with_a_gated_throttle
{
    PerformWorkResult<JobStepResult> _result;

    async Task Establish()
    {
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _throttle.Waiting.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        await _jobStep.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.None, string.Empty), CancellationToken.None);
        _result = await _jobStep.WorkCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_cancel_the_work() => _result.Error.ShouldEqual(PerformWorkError.Cancelled);
    [Fact] void should_leave_the_step_scheduled_for_recovery() => _persistedState.Status.ShouldEqual(JobStepStatus.Scheduled);
    [Fact] async Task should_not_report_a_stop_to_the_job() => await _jobStep.ReportedJob.DidNotReceive().OnStepStopped(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>());
    [Fact] async Task should_not_report_failure_to_the_job() => await _jobStep.ReportedJob.DidNotReceive().OnStepFailed(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>());
    [Fact] void should_not_perform_the_step() => _jobStep.Performed.Task.IsCompleted.ShouldBeFalse();
    [Fact] void should_not_release_an_unacquired_slot() => _throttle.Released.ShouldEqual(0);
}
