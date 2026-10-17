// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;
using Orleans.TestKit;

namespace Cratis.Orleans.Jobs.for_Job.when_draining;

public class and_every_step_drains : given.a_running_job_with_two_steps
{
    JobDrainStatus _result;

    void Establish()
    {
        DrainsAs(_first, _firstId, JobStepStatus.Stopped, Stopped);
        DrainsAs(_second, _secondId, JobStepStatus.CompletedSuccessfully, JobStepResult.Succeeded());
        _silo.StorageStats<given.SomeJob, given.SomeJobState>().ResetCounts();
    }

    async Task Because() => _result = await _job.StopAndDrain(TimeSpan.FromSeconds(5));

    [Fact] void should_be_drained() => _result.ShouldEqual(JobDrainStatus.Drained);
    [Fact] void should_be_stopped() => _job.CurrentState.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_record_one_stop() => _job.CurrentState.StatusChanges.Count(_ => _.Status == JobStatus.Stopped).ShouldEqual(1);
    [Fact] void should_have_settled_the_drain() => _job.CurrentState.DrainPending.ShouldBeFalse();
    [Fact] void should_write_the_stop_and_the_settled_drain() => Writes.ShouldEqual(2);
    [Fact] void should_count_the_stopped_step() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(1);
    [Fact] void should_count_the_succeeded_step() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_hand_each_outcome_to_the_job_once() => _job.HandledStepResults.ShouldContainOnly(_firstId, _secondId);
    [Fact] void should_run_the_stopped_hook_once() => _job.StoppedCount.ShouldEqual(1);
    [Fact] void should_not_notify_the_steps() => _first.Verify(_ => _.OnJobStopped(), Times.Never);
}
