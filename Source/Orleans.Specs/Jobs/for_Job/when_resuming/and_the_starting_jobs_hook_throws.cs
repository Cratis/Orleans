// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_starting_jobs_hook_throws : given.an_interrupted_job_rejecting_resume
{
    void Establish() => _job.CurrentState.Status = JobStatus.StartingSteps;

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_report_failure() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_preserve_starting_steps() => _job.CurrentState.Status.ShouldEqual(JobStatus.StartingSteps);
    [Fact] void should_not_begin_a_drain() => _job.CurrentState.DrainPending.ShouldBeFalse();
    [Fact] void should_not_launch_work() => _unfinished.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
}
