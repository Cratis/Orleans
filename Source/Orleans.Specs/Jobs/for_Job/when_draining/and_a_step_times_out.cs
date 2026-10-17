// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_draining;

public class and_a_step_times_out : given.a_running_job_with_two_steps
{
    JobDrainStatus _result;

    void Establish()
    {
        DrainsAs(_first, _firstId, JobStepStatus.Stopped, Stopped);
        DoesNotDrain(_second);
    }

    async Task Because() => _result = await _job.StopAndDrain(TimeSpan.FromSeconds(5));

    [Fact] void should_time_out() => _result.ShouldEqual(JobDrainStatus.TimedOut);
    [Fact] void should_be_stopped() => _job.CurrentState.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_keep_the_drain_pending() => _job.CurrentState.DrainPending.ShouldBeTrue();
    [Fact] void should_count_the_drained_step() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(1);
    [Fact] void should_hand_only_the_drained_outcome_to_the_job() => _job.HandledStepResults.ShouldContainOnly(_firstId);
    [Fact] void should_not_run_the_stopped_hook() => _job.StoppedCount.ShouldEqual(0);
}
