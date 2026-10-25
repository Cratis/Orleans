// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_activating;

public class in_starting_steps_without_a_watchdog : given.a_job_with_a_durable_plan
{
    protected override JobsOptions CreateOptions() => new() { MaxConcurrentStepStarts = 1, JobWatchdogInterval = TimeSpan.Zero };

    async Task Establish()
    {
        _job.BeforeStarting = () => Task.FromException(new InvalidOperationException("The silo stopped"));
        await _job.Start(new());
        await ActivateFrom(JobStatus.StartingSteps);
        _job.BeforeResuming = () => Task.FromException(new InvalidOperationException("Not yet"));
    }

    async Task Because() => _ = await _job.Resume();

    [Fact] void should_not_arm_a_watchdog() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
    [Fact] void should_stay_starting_steps() => _job.CurrentState.Status.ShouldEqual(JobStatus.StartingSteps);
}
