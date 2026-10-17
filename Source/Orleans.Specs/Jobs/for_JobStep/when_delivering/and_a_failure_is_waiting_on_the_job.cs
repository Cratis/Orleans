// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_delivering;

public class and_a_failure_is_waiting_on_the_job : given.a_step_that_retries_delivery_without_waiting
{
    readonly TaskCompletionSource<Result<JobError>> _parentReply = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _reported = new(TaskCreationOptions.RunContinuationsAsynchronously);
    JobStepStatus _statusWhileTheJobWasStillAnswering;

    async Task Establish()
    {
        _jobStep.ReportedJob.OnStepFailed(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>()).Returns(_ =>
        {
            _statusWhileTheJobWasStillAnswering = _storage.Persisted[^1].Status;
            _reported.TrySetResult();
            return _parentReply.Task;
        });
        _jobStep.PerformedResult = JobStepResult.Failed("failed");
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _reported.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        _parentReply.TrySetResult(Result<JobError>.Success());
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_have_persisted_the_failure_before_the_job_answered() => _statusWhileTheJobWasStillAnswering.ShouldEqual(JobStepStatus.CompletedWithFailure);
    [Fact] async Task should_report_the_failure_once() => await _jobStep.ReportedJob.Received(1).OnStepFailed(_jobStepId, Arg.Any<JobStepResult>());
    [Fact] void should_persist_the_failure_once() => _storage.Persisted.Count(_ => _.Status == JobStepStatus.CompletedWithFailure).ShouldEqual(1);
}
