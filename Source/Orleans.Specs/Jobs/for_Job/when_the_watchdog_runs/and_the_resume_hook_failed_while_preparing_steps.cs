// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_the_watchdog_runs;

public class and_the_resume_hook_failed_while_preparing_steps : given.a_job_with_a_durable_plan
{
    int _timers;

    async Task Establish()
    {
        InterruptPreparation();
        await _job.Start(new());
        await ActivateFrom(JobStatus.PreparingSteps);
        _job.BeforeResuming = () => Task.FromException(new InvalidOperationException("Not yet"));
        (await _job.Resume()).IsSuccess.ShouldBeFalse();
        _timers = _silo.TimerRegistry.NumberOfActiveTimers;
        _job.BeforeResuming = null;
    }

    Task Because() => _job.RunWatchdog();

    [Fact] void should_have_kept_a_watchdog() => _timers.ShouldBeGreaterThan(0);
    [Fact] void should_finish_startup() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_retry_the_resume_hook() => _job.BeforeResumingCount.ShouldEqual(2);
    [Fact] void should_dispatch_the_first_stage() => _dispatched.ShouldContain(_firstId);
}
