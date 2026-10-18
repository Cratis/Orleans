// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Job.when_starting;

public class and_dispatch_requires_initialization : given.a_job_with_a_durable_plan
{
    Result<StartJobError> _start;

    void Establish() => _job.BeforeDispatching = context =>
    {
        _job.BeforeStartingCount.ShouldEqual(1);
        _dispatched.ShouldBeEmpty();
        return Task.CompletedTask;
    };

    async Task Because() => _start = await _job.Start(new());

    [Fact] void should_start() => Assert.True(_start.IsSuccess, string.Join("; ", _job.CurrentState.StatusChanges.SelectMany(change => change.ExceptionMessages)));
    [Fact] void should_run_the_hook_on_clean_start() => _job.DispatchContexts.Single().InitializationMayBeIncomplete.ShouldBeFalse();
    [Fact] void should_save_the_plan_before_preparation() => _job.WrittenStates.First(state => state.Status is JobStatus.PreparingSteps).StepPlan!.Count.ShouldEqual(3);
    [Fact] void should_dispatch_after_initialization() => _dispatched.ShouldEqual(new[] { _firstId, _secondId });
}
