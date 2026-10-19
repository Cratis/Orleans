// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_finalizing_a_stop;

public class and_the_tracked_step_reports_on_its_own : given.a_job_whose_silo_went_away_while_stopping
{
    async Task Establish()
    {
        StoredJobStep(_stepId).Status = JobStepStatus.Running;
        await _job.FinalizeStop();
    }

    async Task Because()
    {
        StoredJobStep(_stepId).Status = JobStepStatus.Stopped;
        await _job.OnStepStopped(_stepId, Stopped);
    }

    [Fact] void should_run_the_stopped_hook_once() => _job.StoppedCount.ShouldEqual(1);
    [Fact] void should_count_the_step_once() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(1);
    [Fact] void should_record_the_stop_as_finalized() => _job.WrittenStates[^1].StopFinalized.ShouldBeTrue();
    [Fact] void should_leave_the_job_stopped() => _job.CurrentState.Status.ShouldEqual(JobStatus.Stopped);
}
