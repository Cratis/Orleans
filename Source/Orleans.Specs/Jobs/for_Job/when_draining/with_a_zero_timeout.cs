// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_draining;

public class with_a_zero_timeout : given.a_running_job_with_two_steps
{
    JobDrainStatus _poll;
    JobDrainStatus _result;

    async Task Establish()
    {
        DrainsAs(_first, _firstId, JobStepStatus.Stopped, Stopped);
        DoesNotDrain(_second);
        _poll = await _job.StopAndDrain(TimeSpan.Zero);
        DrainsAs(_second, _secondId, JobStepStatus.Stopped, Stopped);
    }

    async Task Because() => _result = await _job.StopAndDrain(TimeSpan.Zero);

    [Fact] void should_time_out_the_first_poll() => _poll.ShouldEqual(JobDrainStatus.TimedOut);
    [Fact] void should_be_drained_by_the_second_poll() => _result.ShouldEqual(JobDrainStatus.Drained);
    [Fact] void should_ask_each_step_with_a_zero_timeout() => _first.Verify(_ => _.Drain(It.IsAny<long>(), TimeSpan.Zero), Times.Once);
    [Fact] void should_not_drain_an_accounted_step_again() => _first.Verify(_ => _.Drain(It.IsAny<long>(), It.IsAny<TimeSpan>()), Times.Once);
    [Fact] void should_drain_the_remaining_step_again() => _second.Verify(_ => _.Drain(It.IsAny<long>(), It.IsAny<TimeSpan>()), Times.Exactly(2));
    [Fact] void should_count_both_steps_once() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(2);
}
