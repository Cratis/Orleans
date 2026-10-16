// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_invoking_on_activation;

public class after_admission_closed : given.a_job_step_left_scheduled_by_a_dead_process
{
    readonly TaskCompletionSource _releaseLeftover = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task<Result<bool, ActivationCallbackRejected>> _leftover;
    Result<bool, ActivationCallbackRejected> _result;
    bool _ran;

    async Task Establish()
    {
        _jobStep.HoldPerformOpen = true;
        _jobStep.Work = (step, _) =>
        {
            _leftover = Task.Run(async () =>
            {
                await _releaseLeftover.Task;
                return await step.OnActivation(() => Task.FromResult(_ran = true));
            });
            return Task.CompletedTask;
        };
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _jobStep.Performed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

        var stopping = _jobStep.Stop(false);
        await _jobStep.PerformCancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _jobStep.Gate.TrySetResult();
        (await stopping.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System)).IsSuccess.ShouldBeTrue();
    }

    async Task Because()
    {
        _releaseLeftover.TrySetResult();
        _result = await _leftover.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_refuse_the_callback_as_closed() => _result.AsT1.ShouldEqual(ActivationCallbackRejected.Closed);
    [Fact] void should_not_run_the_callback() => _ran.ShouldBeFalse();

    void Destroy()
    {
        _releaseLeftover.TrySetResult();
        _jobStep.Gate.TrySetResult();
        _jobStep.Dispose();
    }
}
