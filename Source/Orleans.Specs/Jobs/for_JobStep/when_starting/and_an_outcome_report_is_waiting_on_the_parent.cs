// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Orleans.Concurrency;

namespace Cratis.Orleans.Jobs.for_JobStep.when_starting;

public class and_an_outcome_report_is_waiting_on_the_parent : given.a_job_step_with_a_gated_throttle
{
    readonly TaskCompletionSource<Result<JobError>> _parentReply = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task<Result<JobStepError>> _report;
    Result<StartJobStepError> _result;
    bool _reportWasStillPending;

    async Task Establish()
    {
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _throttle.Waiting.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _jobStep.ReportedJob.OnStepFailed(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>()).Returns(_parentReply.Task);
        _report = _jobStep.ReportFailure(PerformJobStepError.Failed(["failed"], string.Empty));
    }

    async Task Because()
    {
        _result = await _jobStep.Start(GrainId.Create("job", _jobId.ToString())).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _reportWasStillPending = !_report.IsCompleted;
    }

    [Fact] void should_answer_liveness_without_waiting_on_the_job_callback() => ((StartJobStepError)_result).ShouldEqual(StartJobStepError.AlreadyStarted);
    [Fact] void should_leave_the_outcome_report_waiting() => _reportWasStillPending.ShouldBeTrue();
    [Fact] void should_allow_the_liveness_call_to_enter_a_step_waiting_on_the_parent() => typeof(IJobStep).GetMethod(nameof(IJobStep.Start))!.IsDefined(typeof(AlwaysInterleaveAttribute), inherit: true).ShouldBeTrue();

    async Task Destroy()
    {
        _parentReply.TrySetResult(Result<JobError>.Success());
        await _report;
        await _jobStep.Stop(false);
    }
}
