// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_hook_throws_while_removing : given.a_stopped_job_rejecting_resume
{
    void Establish() => _job.CurrentState.Status = JobStatus.Removing;

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_report_failure() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_compensate_removal() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_write_only_running() => Writes.ShouldEqual(_writesBefore + 1);
    [Fact] void should_not_dispatch_work() => _step.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
}
