// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_finalizing_a_stop;

public class and_it_is_called_again : given.a_job_whose_silo_went_away_while_stopping
{
    async Task Because()
    {
        await _job.FinalizeStop();
        await _job.FinalizeStop();
    }

    [Fact] void should_run_the_stopped_hook_once() => _job.StoppedCount.ShouldEqual(1);
    [Fact] void should_leave_the_job_stopped() => _job.CurrentState.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_count_the_step_once() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(1);
}
