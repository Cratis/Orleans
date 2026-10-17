// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_activating;

public class with_a_pending_drain : given.a_job_activated_with_a_pending_drain
{
    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_settle_the_drain() => _job.CurrentState.DrainPending.ShouldBeFalse();
    [Fact] void should_stay_stopped() => _job.CurrentState.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_drain_the_step_that_was_running() => _running.Verify(_ => _.Drain(It.IsAny<long>(), It.IsAny<TimeSpan>()), Times.Once);
    [Fact] void should_count_both_steps_as_stopped() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(2);
    [Fact] void should_run_the_stopped_hook() => _job.StoppedCount.ShouldEqual(1);
    [Fact] void should_not_start_any_step() => _running.Verify(_ => _.Start(It.IsAny<GrainId>()), Times.Never);
}
