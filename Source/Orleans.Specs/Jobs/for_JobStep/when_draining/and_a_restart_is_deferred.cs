// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_draining;

public class and_a_restart_is_deferred : given.a_job_step_left_scheduled_by_a_dead_process
{
    const string GenerationKey = "Cratis.Orleans.Jobs.Generation";

    readonly TaskCompletionSource<Result<JobError>> _parentReply = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _reported = new(TaskCreationOptions.RunContinuationsAsynchronously);
    JobStepDrainResult _result;

    async Task Establish()
    {
        // A start of generation 2 is deferred while the stop of the generation 1 run waits on the job.
        _jobStep.ReportedJob.OnStepStopped(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>()).Returns(_ =>
        {
            _reported.TrySetResult();
            return _parentReply.Task;
        });
        _jobStep.HoldPerformOpen = true;
        RequestContext.Set(GenerationKey, 1L);
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _jobStep.Performed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        RequestContext.Set(GenerationKey, 1L);
        await _jobStep.OnJobStopped();
        _jobStep.Gate.TrySetResult();
        await _reported.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        RequestContext.Set(GenerationKey, 2L);
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        RequestContext.Remove(GenerationKey);
    }

    async Task Because()
    {
        _result = await _jobStep.Drain(2, TimeSpan.FromSeconds(5)).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _parentReply.TrySetResult(Result<JobError>.Success());
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_be_drained() => _result.Drained.ShouldBeTrue();
    [Fact] void should_answer_stopped() => _result.Status.ShouldEqual(JobStepStatus.Stopped);
    [Fact] void should_not_run_the_deferred_start() => _jobStep.WorkRuns.ShouldEqual(1);

    async Task Destroy()
    {
        RequestContext.Remove(GenerationKey);
        _parentReply.TrySetResult(Result<JobError>.Success());
        await _jobStep.OutcomeDelivery;
        _jobStep.Dispose();
    }
}
