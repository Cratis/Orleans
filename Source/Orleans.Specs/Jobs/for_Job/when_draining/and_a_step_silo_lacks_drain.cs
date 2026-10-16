// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_draining;

public class and_a_step_silo_lacks_drain : given.a_running_job_with_two_steps
{
    JobDrainStatus _result;

    void Establish()
    {
        DrainsAs(_first, _firstId, JobStepStatus.Stopped, Stopped);
        _second.Setup(_ => _.Drain(It.IsAny<long>(), It.IsAny<TimeSpan>())).ThrowsAsync(new NotSupportedException("The grain does not implement the interface"));
    }

    async Task Because() => _result = await _job.StopAndDrain(TimeSpan.FromSeconds(5));

    [Fact] void should_time_out() => _result.ShouldEqual(JobDrainStatus.TimedOut);
    [Fact] void should_keep_the_drain_pending() => _job.CurrentState.DrainPending.ShouldBeTrue();
    [Fact] void should_count_only_the_drained_step() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(1);
}
