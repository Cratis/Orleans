// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_the_watchdog_runs;

public class and_the_resume_hook_failed_while_starting_steps : given.a_job_with_a_durable_plan
{
    async Task Establish()
    {
        _job.BeforeStarting = () => Task.FromException(new InvalidOperationException("The silo stopped"));
        await _job.Start(new());
        await ActivateFrom(JobStatus.StartingSteps);
        _job.BeforeResuming = () => Task.FromException(new InvalidOperationException("Not yet"));
        (await _job.Resume()).IsSuccess.ShouldBeFalse();
        _job.CurrentState.Status.ShouldEqual(JobStatus.StartingSteps);
        _job.BeforeResuming = null;
    }

    Task Because() => _job.RunWatchdog();

    [Fact] void should_retry_the_resume_hook() => _job.BeforeResumingCount.ShouldEqual(2);
    [Fact] void should_finish_startup() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_start_the_first_stage() => _dispatched.ShouldEqual(new[] { _firstId, _secondId });
    [Fact] void should_not_start_the_later_stage() => _dispatched.ShouldNotContain(_laterId);
}
