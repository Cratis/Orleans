// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_draining;

public class and_the_stop_cannot_be_persisted : given.a_running_job_with_two_steps
{
    Exception _error;

    void Establish()
    {
        DrainsAs(_first, _firstId, JobStepStatus.Stopped, Stopped);
        DrainsAs(_second, _secondId, JobStepStatus.Stopped, Stopped);
        _job.FailWrites = true;
    }

    async Task Because() => _error = await Catch.Exception(() => _job.StopAndDrain(TimeSpan.FromSeconds(5)));

    [Fact] void should_fail() => _error.ShouldNotBeNull();
    [Fact] void should_not_drain_any_step() => _first.Verify(_ => _.Drain(It.IsAny<long>(), It.IsAny<TimeSpan>()), Times.Never);
    [Fact] void should_stay_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_not_have_a_pending_drain() => _job.CurrentState.DrainPending.ShouldBeFalse();
}
