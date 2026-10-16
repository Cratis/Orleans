// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_starting;

public class while_a_stop_of_an_older_generation_is_pending : given.a_job_step_left_scheduled_by_a_dead_process
{
    const string GenerationKey = "Cratis.Orleans.Jobs.Generation";

    readonly TaskCompletionSource<Result<JobError>> _parentReply = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _reported = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _succeeded = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Result<StartJobStepError> _result;
    int _workRunsBeforeTheStopWasDelivered;
    int _deactivationsWhileDeferred;

    async Task Establish()
    {
        // The run of generation 1 is stopped by its job, and its stop report waits on the job.
        _jobStep.ReportedJob.OnStepStopped(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>()).Returns(_ =>
        {
            _reported.TrySetResult();
            return _parentReply.Task;
        });
        _jobStep.ReportedJob.OnStepSucceeded(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>()).Returns(_ =>
        {
            _succeeded.TrySetResult();
            return Task.FromResult(Result<JobError>.Success());
        });
        _jobStep.HoldPerformOpen = true;
        await WithGeneration(1, () => _jobStep.Start(GrainId.Create("job", _jobId.ToString())));
        await _jobStep.Performed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await WithGeneration(1, _jobStep.OnJobStopped);
        _jobStep.HoldPerformOpen = false;
        _jobStep.Gate.TrySetResult();
        await _reported.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Because()
    {
        // The job is resumed while the stop is pending, which begins generation 2.
        _result = await WithGeneration(2, () => _jobStep.Start(GrainId.Create("job", _jobId.ToString())));
        _workRunsBeforeTheStopWasDelivered = _jobStep.WorkRuns;
        _parentReply.TrySetResult(Result<JobError>.Success());
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _deactivationsWhileDeferred = _silo.GrainRuntime.Mock.Invocations.Count(_ => _.Method.Name == nameof(IGrainRuntime.DeactivateOnIdle));
        await _silo.TimerRegistry.FireAllAsync();
        await _jobStep.SecondWorkRunEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _succeeded.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_accept_the_start() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_run_while_the_stop_is_pending() => _workRunsBeforeTheStopWasDelivered.ShouldEqual(1);
    [Fact] void should_not_deactivate_while_the_start_is_deferred() => _deactivationsWhileDeferred.ShouldEqual(0);
    [Fact] void should_run_once_the_stop_has_settled() => _jobStep.WorkRuns.ShouldEqual(2);
    [Fact] async Task should_report_the_stop_once() => await _jobStep.ReportedJob.Received(1).OnStepStopped(_jobStepId, Arg.Any<JobStepResult>());
    [Fact] async Task should_report_the_new_run() => await _jobStep.ReportedJob.Received(1).OnStepSucceeded(_jobStepId, Arg.Any<JobStepResult>());

    static async Task<T> WithGeneration<T>(long generation, Func<Task<T>> call)
    {
        RequestContext.Set(GenerationKey, generation);
        try
        {
            return await call();
        }
        finally
        {
            RequestContext.Remove(GenerationKey);
        }
    }

    static Task WithGeneration(long generation, Func<Task> call) => WithGeneration(generation, async () =>
    {
        await call();
        return true;
    });

    async Task Destroy()
    {
        _parentReply.TrySetResult(Result<JobError>.Success());
        _jobStep.Gate.TrySetResult();
        await _jobStep.OutcomeDelivery;
        _jobStep.Dispose();
    }
}
