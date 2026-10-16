// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class and_the_step_succeeded_before_the_stop : given.a_job_step_left_scheduled_by_a_dead_process
{
    Result<JobStepError> _stopped;

    async Task Establish()
    {
        // The work finishes successfully although the stop has already cancelled it.
        _jobStep.HoldPerformOpen = true;
        _jobStep.SucceedEvenIfCancelled = true;
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _jobStep.Performed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        var stopping = _jobStep.Stop(false);
        await _jobStep.PerformCancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _jobStep.Gate.TrySetResult();
        _stopped = await stopping.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_succeed() => _stopped.IsSuccess.ShouldBeTrue();
    [Fact] void should_record_the_step_as_completed_successfully() => _persistedState.Status.ShouldEqual(JobStepStatus.CompletedSuccessfully);
    [Fact] async Task should_report_the_success_to_the_job_once() => await _jobStep.ReportedJob.Received(1).OnStepSucceeded(_jobStepId, Arg.Any<JobStepResult>());
    [Fact] async Task should_not_report_a_stop_to_the_job() => await _jobStep.ReportedJob.DidNotReceive().OnStepStopped(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>());

    void Destroy()
    {
        _jobStep.Gate.TrySetResult();
        _jobStep.Dispose();
    }
}
