// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_running_with_a_window;

public class and_a_step_completes_on_a_cold_job_without_a_watchdog : given.a_window_of_two_over_five_steps
{
    protected override JobsOptions CreateOptions() => new() { JobWatchdogInterval = TimeSpan.Zero };

    async Task Establish() => await _job.OnActivateAsync(CancellationToken.None);

    Task Because() => Succeed(0);

    [Fact] void should_start_the_next_step() => StartsOf(2).ShouldEqual(1);
    [Fact] void should_keep_the_window_full() => Started.ShouldEqual(3);
}
