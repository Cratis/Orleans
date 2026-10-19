// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class for_removal_and_the_job_rejects_the_stop_report : given.a_job_step_with_a_gated_throttle
{
    Result<JobStepError> _stopped;

    async Task Establish()
    {
        _jobStep.ReportedJob.OnStepStopped(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>()).Returns(Result<JobError>.Failed(JobError.UnknownError));
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _throttle.Waiting.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        _stopped = await _jobStep.Stop(true).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_succeed() => _stopped.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_attempt_to_report_the_stop_once() => await _jobStep.ReportedJob.Received(1).OnStepStopped(_jobStepId, Arg.Any<JobStepResult>());
    [Fact] void should_not_record_the_step_as_stopped() => _storage.Persisted.Any(_ => _.Status == JobStepStatus.Stopped).ShouldBeFalse();
    [Fact] void should_still_remove_the_step() => _storage.Persisted[^1].Status.ShouldEqual(JobStepStatus.Removing);
}
