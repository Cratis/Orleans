// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;
using Orleans.TestKit;

namespace Cratis.Orleans.Jobs.for_Job.when_draining;

public class and_it_was_already_drained : given.a_running_job_with_two_steps
{
    JobDrainStatus _result;

    async Task Establish()
    {
        DrainsAs(_first, _firstId, JobStepStatus.Stopped, Stopped);
        DrainsAs(_second, _secondId, JobStepStatus.Stopped, Stopped);
        (await _job.StopAndDrain(TimeSpan.Zero)).ShouldEqual(JobDrainStatus.Drained);
        _silo.StorageStats<given.SomeJob, given.SomeJobState>().ResetCounts();
    }

    async Task Because() => _result = await _job.StopAndDrain(TimeSpan.Zero);

    [Fact] void should_be_drained() => _result.ShouldEqual(JobDrainStatus.Drained);
    [Fact] void should_not_write() => Writes.ShouldEqual(0);
    [Fact] void should_not_run_the_stopped_hook_again() => _job.StoppedCount.ShouldEqual(1);
    [Fact] void should_not_drain_the_steps_again() => _first.Verify(_ => _.Drain(It.IsAny<long>(), It.IsAny<TimeSpan>()), Times.Once);
}
