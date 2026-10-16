// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_starting;

public class while_the_previous_delivery_is_unsettled : given.a_job_step_left_scheduled_by_a_dead_process
{
    readonly TaskCompletionSource<Result<JobError>> _parentReply = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _reported = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Result<StartJobStepError> _result;

    async Task Establish()
    {
        // The run's work ends stopped on its own and its report waits on the job.
        _jobStep.ReportedJob.OnStepStopped(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>()).Returns(_ =>
        {
            _reported.TrySetResult();
            return _parentReply.Task;
        });
        _jobStep.PerformedResult = JobStepResult.Failed(PerformJobStepError.CancelledWithNoResult());
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _jobStep.WorkCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _reported.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because() => _result = await _jobStep.Start(GrainId.Create("job", _jobId.ToString())).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

    [Fact] void should_refuse_as_already_started() => ((StartJobStepError)_result).ShouldEqual(StartJobStepError.AlreadyStarted);
    [Fact] void should_not_launch_new_work() => _jobStep.WorkRuns.ShouldEqual(1);
    [Fact] void should_leave_the_delivery_waiting_on_the_job() => _jobStep.OutcomeDelivery.IsCompleted.ShouldBeFalse();

    async Task Destroy()
    {
        _parentReply.TrySetResult(Result<JobError>.Success());
        await _jobStep.OutcomeDelivery;
        _jobStep.Dispose();
    }
}
