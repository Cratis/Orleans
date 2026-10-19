// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_stops;

public class and_finishing_the_stop_fails : given.a_job_whose_silo_went_away_while_stopping
{
    void Establish()
    {
        StoredJobStep(_stepId).Status = JobStepStatus.Running;
        _job.OnStoppedThrows = true;
    }

    Task Because() => _job.OnStepStopped(_stepId, Stopped);

    [Fact] void should_run_the_stopped_hook() => _job.StoppedCount.ShouldEqual(1);
    [Fact] void should_not_schedule_a_retry_of_finishing_a_removal() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
