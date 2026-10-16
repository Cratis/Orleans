// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_deriving_the_generation;

public class after_cold_rehydration : given.a_job_whose_steps_report_the_generation_they_are_started_in
{
    void Establish() => WithStatusHistory(JobStatus.PreparingJob, JobStatus.PreparingSteps, JobStatus.StartingSteps, JobStatus.Running);

    Task Because() => _job.Resume();

    [Fact] void should_continue_the_execution_the_steps_belong_to() => _startedIn.ShouldContainOnly(1L);
}
