// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workers;

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class and_the_step_has_just_been_started : given.a_job_step_with_a_gated_throttle
{
    PerformWorkResult<JobStepResult> _result;

    async Task Because()
    {
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        (await _jobStep.Stop(false)).IsSuccess.ShouldBeTrue();
        _result = await _jobStep.WorkCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_cancel_the_work() => _result.Error.ShouldEqual(PerformWorkError.Cancelled);
    [Fact] void should_report_the_step_as_stopped() => _persistedState.Status.ShouldEqual(JobStepStatus.Stopped);
    [Fact] async Task should_report_the_stop_to_the_job_once() => await _jobStep.ReportedJob.Received(1).OnStepStopped(_jobStepId, Arg.Any<JobStepResult>());
    [Fact] async Task should_not_report_failure_to_the_job() => await _jobStep.ReportedJob.DidNotReceive().OnStepFailed(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>());
    [Fact] void should_not_perform_the_step() => _jobStep.Performed.Task.IsCompleted.ShouldBeFalse();
    [Fact] void should_not_release_an_unacquired_slot() => _throttle.Released.ShouldEqual(0);
}
