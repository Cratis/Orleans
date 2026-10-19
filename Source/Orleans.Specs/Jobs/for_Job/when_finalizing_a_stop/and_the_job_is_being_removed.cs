// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Cratis.Orleans.Jobs.for_Job.when_finalizing_a_stop;

public class and_the_job_is_being_removed : given.a_job_whose_silo_went_away_while_stopping
{
    void Establish()
    {
        _job.CurrentState.Status = JobStatus.Removing;
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Removing });
    }

    Task Because() => _job.FinalizeStop();

    [Fact] void should_run_the_stopped_hook_once() => _job.StoppedCount.ShouldEqual(1);
    [Fact] void should_complete_the_removal_by_clearing_the_job() => _silo.StorageStats<given.SomeJob, given.SomeJobState>().Clears.ShouldEqual(1);
}
