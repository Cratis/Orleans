// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_draining;

public class and_the_worker_recorded_success : given.a_job_step_left_scheduled_by_a_dead_process
{
    readonly TaskCompletionSource<Result<JobError>> _parentReply = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _reported = new(TaskCreationOptions.RunContinuationsAsynchronously);
    JobStepDrainResult _result;

    async Task Establish()
    {
        // The success report is queued behind the job's drain, as it is when the job drains its steps.
        _jobStep.ReportedJob.OnStepSucceeded(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>()).Returns(_ =>
        {
            _reported.TrySetResult();
            return _parentReply.Task;
        });
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _reported.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because() => _result = await _jobStep.Drain(1, TimeSpan.FromSeconds(5)).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

    [Fact] void should_be_drained() => _result.Drained.ShouldBeTrue();
    [Fact] void should_answer_completed_successfully() => _result.Status.ShouldEqual(JobStepStatus.CompletedSuccessfully);
    [Fact] void should_answer_the_success() => _result.Outcome!.TryGetError(out _).ShouldBeFalse();
    [Fact] void should_have_persisted_the_success_once() => _storage.Persisted.Count(_ => _.Status == JobStepStatus.CompletedSuccessfully).ShouldEqual(1);
    [Fact] void should_leave_the_report_waiting_on_the_job() => _jobStep.OutcomeDelivery.IsCompleted.ShouldBeFalse();

    async Task Destroy()
    {
        _parentReply.TrySetResult(Result<JobError>.Success());
        await _jobStep.OutcomeDelivery;
        _jobStep.Dispose();
    }
}
