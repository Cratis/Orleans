// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_draining;

public class and_a_failure_report_waits_on_the_job : given.a_job_step_left_scheduled_by_a_dead_process
{
    readonly TaskCompletionSource<Result<JobError>> _parentReply = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _reported = new(TaskCreationOptions.RunContinuationsAsynchronously);
    JobStepDrainResult _result;

    async Task Establish()
    {
        // A failure is reported before it is persisted, and the report is queued behind the job's drain.
        _jobStep.ReportedJob.OnStepFailed(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>()).Returns(_ =>
        {
            _reported.TrySetResult();
            return _parentReply.Task;
        });
        _jobStep.PerformedResult = JobStepResult.Failed(PerformJobStepError.Failed(["it broke"], string.Empty));
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _reported.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        _result = await _jobStep.Drain(1, TimeSpan.FromSeconds(5)).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _parentReply.TrySetResult(Result<JobError>.Success());
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_be_drained() => _result.Drained.ShouldBeTrue();
    [Fact] void should_answer_completed_with_failure() => _result.Status.ShouldEqual(JobStepStatus.CompletedWithFailure);
    [Fact] void should_record_the_failure_once() => _persistedState.StatusChanges.Count(_ => _.Status == JobStepStatus.CompletedWithFailure).ShouldEqual(1);
    [Fact] void should_persist_the_failure_once() => _storage.Persisted.Count(_ => _.Status == JobStepStatus.CompletedWithFailure).ShouldEqual(1);

    async Task Destroy()
    {
        _parentReply.TrySetResult(Result<JobError>.Success());
        await _jobStep.OutcomeDelivery;
        _jobStep.Dispose();
    }
}
