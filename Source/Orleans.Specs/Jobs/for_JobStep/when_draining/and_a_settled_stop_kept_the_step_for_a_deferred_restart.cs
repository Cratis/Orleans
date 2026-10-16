// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_draining;

/// <summary>
/// The stop of generation 1 has settled and reported, and the step stays activated only to run the start of
/// generation 2 it deferred, which has not had its turn yet.
/// </summary>
public class and_a_settled_stop_kept_the_step_for_a_deferred_restart : given.a_job_step_left_scheduled_by_a_dead_process
{
    const string GenerationKey = "Cratis.Orleans.Jobs.Generation";

    readonly TaskCompletionSource<Result<JobError>> _parentReply = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _reported = new(TaskCreationOptions.RunContinuationsAsynchronously);
    JobStepDrainResult _result;
    int _deactivationsBeforeTheDrain;

    async Task Establish()
    {
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
        _parentReply.TrySetResult(Result<JobError>.Success());
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _deactivationsBeforeTheDrain = Deactivations();
    }

    async Task Because()
    {
        _result = await _jobStep.Drain(2, TimeSpan.FromSeconds(5)).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_be_drained() => _result.Drained.ShouldBeTrue();
    [Fact] void should_have_kept_the_step_for_the_deferred_start() => _deactivationsBeforeTheDrain.ShouldEqual(0);
    [Fact] void should_deactivate_once_drained() => Deactivations().ShouldEqual(1);
    [Fact] void should_not_run_the_deferred_start() => _jobStep.WorkRuns.ShouldEqual(1);

    int Deactivations() => _silo.GrainRuntime.Mock.Invocations.Count(_ => _.Method.Name == nameof(IGrainRuntime.DeactivateOnIdle));

    async Task Destroy()
    {
        RequestContext.Remove(GenerationKey);
        _parentReply.TrySetResult(Result<JobError>.Success());
        await _jobStep.OutcomeDelivery;
        _jobStep.Dispose();
    }
}
