// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_invoking_on_activation;

public class cancelled_while_queued : given.a_job_step_left_scheduled_by_a_dead_process
{
    readonly TaskCompletionSource _holding = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseHolding = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<Task<Result<ActivationCallbackRejected>>> _queued = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly CancellationTokenSource _cancel = new();
    Result<ActivationCallbackRejected> _result;
    bool _ran;

    async Task Establish()
    {
        _jobStep.Work = async (step, _) =>
        {
            var holding = step.OnActivation(async () =>
            {
                _holding.TrySetResult();
                await _releaseHolding.Task;
            });
            await _holding.Task;
            _queued.TrySetResult(step.OnActivation(
                () =>
                {
                    _ran = true;
                    return Task.CompletedTask;
                },
                _cancel.Token));
            await holding;
        };
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
    }

    async Task Because()
    {
        var queued = await _queued.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _cancel.CancelAsync();
        _result = await queued.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _releaseHolding.TrySetResult();
        await _jobStep.WorkCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_refuse_the_callback_as_cancelled() => _result.AsT1.ShouldEqual(ActivationCallbackRejected.Cancelled);
    [Fact] void should_never_run_the_callback() => _ran.ShouldBeFalse();

    void Destroy()
    {
        _releaseHolding.TrySetResult();
        _cancel.Dispose();
        _jobStep.Dispose();
    }
}
