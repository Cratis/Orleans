// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_job_was_left_preparing : given.a_job_left_running
{
    void Establish() => _job.CurrentState.Status = JobStatus.PreparingJob;
    async Task Because() => _result = await _job.Resume();

    [Fact] void should_report_unprepared_job() => _result.AsT1.AsT0.ShouldEqual(CannotResumeJobError.JobIsNotPrepared);
}
