// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;
using Orleans.TestKit;

namespace Cratis.Orleans.Jobs.for_Job.when_draining;

public class and_the_job_completed : given.a_running_job_with_two_steps
{
    JobDrainStatus _result;

    void Establish()
    {
        _job.CurrentState.Status = JobStatus.CompletedSuccessfully;
        _silo.StorageStats<given.SomeJob, given.SomeJobState>().ResetCounts();
    }

    async Task Because() => _result = await _job.StopAndDrain(TimeSpan.FromSeconds(5));

    [Fact] void should_answer_completed() => _result.ShouldEqual(JobDrainStatus.Completed);
    [Fact] void should_not_write() => Writes.ShouldEqual(0);
    [Fact] void should_not_drain_any_step() => _first.Verify(_ => _.Drain(It.IsAny<long>(), It.IsAny<TimeSpan>()), Times.Never);
}
