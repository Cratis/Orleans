// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Workers;

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class and_the_job_rejects_the_stop_report : given.a_job_step_with_a_gated_throttle
{
    PerformWorkResult<JobStepResult> _result;

    async Task Establish()
    {
        _jobStep.ReportedJob.OnStepStopped(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>()).Returns(Result<JobError>.Failed(JobError.UnknownError));
        var started = await _jobStep.Start(GrainId.Create("job", _jobId.ToString()));
        started.IsSuccess.ShouldBeTrue();
        await _throttle.Waiting.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        (await _jobStep.Stop(false)).IsSuccess.ShouldBeTrue();
        _result = await _jobStep.WorkCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_cancel_the_work() => _result.Error.ShouldEqual(PerformWorkError.Cancelled);
    [Fact] void should_not_record_the_step_as_stopped() => _persistedState.Status.ShouldNotEqual(JobStepStatus.Stopped);
    [Fact] async Task should_attempt_to_report_the_stop_once() => await _jobStep.ReportedJob.Received(1).OnStepStopped(_jobStepId, Arg.Any<JobStepResult>());
    [Fact] void should_not_perform_the_step() => _jobStep.Performed.Task.IsCompleted.ShouldBeFalse();
}
