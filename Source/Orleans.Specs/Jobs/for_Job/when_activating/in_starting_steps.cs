// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_activating;

public class in_starting_steps : given.a_job_with_a_durable_plan
{
    async Task Establish()
    {
        _job.BeforeStarting = () => Task.FromException(new InvalidOperationException("The silo stopped"));
        await _job.Start(new());
        await ActivateFrom(JobStatus.StartingSteps);
    }

    [Fact] void should_arm_a_watchdog() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldBeGreaterThan(0);
}
