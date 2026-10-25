// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_the_watchdog_runs;

public class and_the_resume_hook_keeps_failing_while_starting_steps : given.a_job_with_a_durable_plan
{
    async Task Establish()
    {
        _job.BeforeStarting = () => Task.FromException(new InvalidOperationException("The silo stopped"));
        await _job.Start(new());
        await ActivateFrom(JobStatus.StartingSteps);
        _job.BeforeResuming = () => Task.FromException(new InvalidOperationException("Not yet"));
        _ = await _job.Resume();
    }

    async Task Because()
    {
        await _job.RunWatchdog();
        await _job.RunWatchdog();
    }

    [Fact] void should_back_off() => _job.BeforeResumingCount.ShouldEqual(2);
    [Fact] void should_stay_starting_steps() => _job.CurrentState.Status.ShouldEqual(JobStatus.StartingSteps);
    [Fact] void should_not_dispatch() => _dispatched.ShouldBeEmpty();
    [Fact] void should_keep_watching() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldBeGreaterThan(0);
}
