// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Orleans.TestKit;

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_stops;

public class and_the_stopped_job_already_accounted_for_every_step : given.the_job
{
    JobStepId _stepId;
    Result<JobError> _report;

    void Establish()
    {
        // A cold activation reads a durable Stop whose step was already counted by the previous activation.
        _stepId = JobStepId.New();
        _job.CurrentState.Status = JobStatus.Stopped;
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.StartingSteps });
        _job.CurrentState.Progress.TotalSteps = 1;
        _job.CurrentState.Progress.StoppedSteps = 1;
        _silo.StorageStats<given.SomeJob, given.SomeJobState>().ResetCounts();
    }

    async Task Because() => _report = await _job.OnStepStopped(_stepId, JobStepResult.Succeeded());

    [Fact] void should_acknowledge_the_report() => _report.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_count_a_second_stop() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(1);
    [Fact] void should_not_write_state() => _silo.StorageStats<given.SomeJob, given.SomeJobState>().Writes.ShouldEqual(0);
    [Fact] void should_not_deliver_the_result_again() => _job.HandledStepResults.ShouldBeEmpty();
    [Fact] void should_stay_stopped() => _job.CurrentState.Status.ShouldEqual(JobStatus.Stopped);
}
