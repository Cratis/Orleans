// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_deriving_the_generation;

public class after_resume_from_stopped : given.a_job_whose_steps_report_the_generation_they_are_started_in
{
    void Establish() => WithStatusHistory(JobStatus.PreparingJob, JobStatus.PreparingSteps, JobStatus.StartingSteps, JobStatus.Running, JobStatus.Stopped);

    Task Because() => _job.Resume();

    // The running change the resume writes is the sixth entry in the status history.
    [Fact] void should_start_the_step_in_the_generation_of_the_resume() => _startedIn.ShouldContainOnly(6L);
}
