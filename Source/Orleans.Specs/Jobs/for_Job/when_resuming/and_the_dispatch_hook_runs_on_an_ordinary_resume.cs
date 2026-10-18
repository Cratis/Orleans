// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_dispatch_hook_runs_on_an_ordinary_resume : given.a_job_with_a_durable_plan
{
    async Task Establish()
    {
        await _job.Start(new());
        await ActivateFrom(JobStatus.Running);
        _job.BeforeDispatching = context =>
        {
            _job.BeforeResumingCount.ShouldEqual(1);
            _dispatched.ShouldBeEmpty();
            return Task.CompletedTask;
        };
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_resume() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_pass_false_for_ordinary_resume() => _job.DispatchContexts.Single().InitializationMayBeIncomplete.ShouldBeFalse();
    [Fact] void should_initialize_before_dispatch() => _dispatched.ShouldEqual(new[] { _firstId, _secondId });
    [Fact] void should_not_replan() => _job.PrepareStepsCount.ShouldEqual(0);
}
