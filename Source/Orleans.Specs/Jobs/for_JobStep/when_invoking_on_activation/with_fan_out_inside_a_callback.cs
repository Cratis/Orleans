// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_JobStep.when_invoking_on_activation;

public class with_fan_out_inside_a_callback : given.a_job_step_left_scheduled_by_a_dead_process
{
    readonly List<string> _order = [];
    Result<ActivationCallbackRejected>[] _inner = [];
    int _inside;
    int _mostInside;

    void Establish()
    {
        _jobStep.Work = async (step, _) =>
        {
            var inner = new List<Task<Result<ActivationCallbackRejected>>>();
            await step.OnActivation(async () =>
            {
                Enter("outer");

                // Started but not awaited inside the callback: they must queue behind it, not slip past the gate.
                for (var i = 0; i < 3; i++)
                {
                    var name = $"inner {i}";
                    inner.Add(step.OnActivation(async () =>
                    {
                        Enter(name);
                        await Task.Yield();
                        Leave();
                    }));
                }

                // Widens the window in which a gate bypass would let an inner callback in.
                await Task.Yield();
                Leave();
            });
            _inner = await Task.WhenAll(inner);
        };
    }

    async Task Because()
    {
        (await _jobStep.Start(GrainId.Create("job", _jobId.ToString()))).IsSuccess.ShouldBeTrue();
        await _jobStep.WorkCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _jobStep.OutcomeDelivery.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_run_one_callback_at_a_time() => _mostInside.ShouldEqual(1);
    [Fact] void should_run_every_inner_callback() => _inner.Count(_ => _.IsSuccess).ShouldEqual(3);
    [Fact] void should_run_the_outer_callback_first() => _order[0].ShouldEqual("outer");

    void Enter(string name)
    {
        lock (_order)
        {
            _order.Add(name);
        }
        var inside = Interlocked.Increment(ref _inside);
        _mostInside = Math.Max(_mostInside, inside);
    }

    void Leave() => Interlocked.Decrement(ref _inside);

    void Destroy() => _jobStep.Dispose();
}
