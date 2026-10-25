// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_invoking_on_activation;

public class from_a_stale_run : given.a_job_step_left_scheduled_by_a_dead_process
{
    readonly TaskCompletionSource _releaseLeftover = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task<Result<bool, ActivationCallbackRejected>> _leftover;
    Result<bool, ActivationCallbackRejected> _result;
    bool _ran;

    async Task Establish()
    {
        // The first run leaves work behind and ends stopped, so the step can start again.
        _jobStep.Work = (step, _) =>
        {
            _leftover = Task.Run(async () =>
            {
                await _releaseLeftover.Task;
                return await step.OnActivation(() => Task.FromResult(_ran = true));
            });
            return Task.CompletedTask;
        };
        _jobStep.PerformedResult = JobStepResult.Failed(PerformJobStepError.CancelledWithNoResult());
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _jobStep.WorkCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

        // WorkCompleted is raised from inside the work, before the background task around it has returned; the run is
        // not settled, and a new Start is refused as already started, until that task and the delivery have both settled.
        await _jobStep.CurrentRun!.WorkerSettled.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

        _jobStep.Work = null;
        _jobStep.PerformedResult = null;
        _jobStep.HoldPerformOpen = true;
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
    }

    async Task Because()
    {
        _releaseLeftover.TrySetResult();
        _result = await _leftover.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_refuse_the_callback_as_from_a_stale_run() => _result.AsT1.ShouldEqual(ActivationCallbackRejected.StaleRun);
    [Fact] void should_not_run_the_callback() => _ran.ShouldBeFalse();

    void Destroy()
    {
        _releaseLeftover.TrySetResult();
        _jobStep.Gate.TrySetResult();
        _jobStep.Dispose();
    }
}
