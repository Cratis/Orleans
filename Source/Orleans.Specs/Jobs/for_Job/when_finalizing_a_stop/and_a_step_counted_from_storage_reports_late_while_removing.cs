// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Cratis.Orleans.Jobs.for_Job.when_finalizing_a_stop;

public class and_a_step_counted_from_storage_reports_late_while_removing : given.a_job_whose_silo_went_away_while_stopping
{
    JobStepId _stillStoppingStepId;

    async Task Establish()
    {
        _stillStoppingStepId = Guid.Parse("7d7d7d7d-0000-0000-0000-000000000002");
        StoredJobStep(_stillStoppingStepId).Status = JobStepStatus.Running;
        _job.CurrentState.Progress.TotalSteps = 2;
        _job.CurrentState.Status = JobStatus.Removing;
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Removing });
        await _job.FinalizeStop();
    }

    Task Because() => _job.OnStepStopped(_stepId, Stopped);

    [Fact] void should_count_the_step_once() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(1);
    [Fact] void should_not_run_the_stopped_hook_yet() => _job.StoppedCount.ShouldEqual(0);
    [Fact] void should_keep_waiting_for_the_step_still_stopping() => _silo.StorageStats<given.SomeJob, given.SomeJobState>().Clears.ShouldEqual(0);
}
