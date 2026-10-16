// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_deriving_the_generation;

public class from_state_written_by_an_older_release : given.a_job_whose_steps_report_the_generation_they_are_started_in
{
    void Establish()
    {
        // An older release resumed the job after a stop; it only appended to the status history.
        WithStatusHistory(JobStatus.PreparingJob, JobStatus.PreparingSteps, JobStatus.StartingSteps, JobStatus.Running, JobStatus.Stopped, JobStatus.Running);
    }

    Task Because() => _job.Resume();

    [Fact] void should_start_the_step_in_the_execution_the_older_release_began() => _startedIn.ShouldContainOnly(6L);
}
