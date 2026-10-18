// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_starting_steps_was_just_persisted : given.a_job_with_a_durable_plan
{
    async Task Establish()
    {
        _job.AfterWrite = () => _job.CurrentState.Status is JobStatus.StartingSteps
            ? Task.FromException(new InvalidOperationException("The silo stopped after persisting"))
            : Task.CompletedTask;
        await _job.Start(new());
        await ActivateFrom(JobStatus.StartingSteps);
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_resume() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_replan() => _job.PrepareStepsCount.ShouldEqual(0);
    [Fact] void should_skip_the_start_hook_even_if_it_never_ran() => _job.BeforeStartingCount.ShouldEqual(0);
    [Fact] void should_signal_incomplete_initialization() => _job.DispatchContexts.Single().InitializationMayBeIncomplete.ShouldBeTrue();
    [Fact] void should_reuse_the_step_ids() => _dispatched.ShouldEqual(new[] { _firstId, _secondId });
}
