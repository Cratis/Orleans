// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs.for_JobStepTestRun.when_performing;

public class and_the_step_invokes_on_activation : given.a_job_step_with_a_test_run
{
    int _running;
    int _mostRunningAtOnce;
    readonly List<bool> _ranOnActivation = [];
    Result<bool, ActivationCallbackRejected>[] _results;

    void Establish() =>
        _jobStep.Work = async (step, _) =>
        {
            // Two callbacks posted at once, each yielding while it runs, so overlap would show.
            async Task<bool> Callback()
            {
                _mostRunningAtOnce = Math.Max(_mostRunningAtOnce, Interlocked.Increment(ref _running));
                await Task.Yield();
                _ranOnActivation.Add(_testRun.IsOnActivation);
                Interlocked.Decrement(ref _running);
                return true;
            }

            _results = await Task.WhenAll(step.OnActivation(Callback), step.OnActivation(Callback));
        };

    async Task Because() => await _testRun.Perform(new JobStepState()).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

    [Fact] void should_admit_both_callbacks() => _results.All(_ => _.IsSuccess).ShouldBeTrue();
    [Fact] void should_run_them_on_the_activation() => _ranOnActivation.ShouldEachConformTo(_ => _);
    [Fact] void should_run_them_one_at_a_time() => _mostRunningAtOnce.ShouldEqual(1);
    [Fact] void should_not_run_the_step_itself_on_the_activation() => _testRun.IsOnActivation.ShouldBeFalse();
}
