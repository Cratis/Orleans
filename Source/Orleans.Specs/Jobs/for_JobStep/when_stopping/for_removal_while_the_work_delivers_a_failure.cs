// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_stopping;

public class for_removal_while_the_work_delivers_a_failure : given.a_job_step_left_scheduled_by_a_dead_process
{
    readonly TaskCompletionSource<Result<JobError>> _parentReply = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _reported = new(TaskCreationOptions.RunContinuationsAsynchronously);

    async Task Establish()
    {
        // The work fails on its own and its report waits on the job when the job is removed.
        _jobStep.ReportedJob.OnStepFailed(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>()).Returns(_ =>
        {
            _reported.TrySetResult();
            return _parentReply.Task;
        });
        _jobStep.PerformedResult = JobStepResult.Failed("failed");
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _reported.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        await _jobStep.OnJobRemoved();
        _parentReply.TrySetResult(Result<JobError>.Success());
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] async Task should_report_the_failure_once() => await _jobStep.ReportedJob.Received(1).OnStepFailed(_jobStepId, Arg.Any<JobStepResult>());
    [Fact] async Task should_not_report_a_stop() => await _jobStep.ReportedJob.DidNotReceive().OnStepStopped(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>());
    [Fact] void should_record_the_failure_before_removing() => _storage.Persisted.Select(_ => _.Status).TakeLast(2).ShouldEqual([JobStepStatus.CompletedWithFailure, JobStepStatus.Removing]);
    [Fact] void should_remove_the_step_once() => _storage.Persisted.Count(_ => _.Status == JobStepStatus.Removing).ShouldEqual(1);
}
