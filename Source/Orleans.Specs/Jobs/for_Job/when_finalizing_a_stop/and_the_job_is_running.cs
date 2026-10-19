// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_finalizing_a_stop;

public class and_the_job_is_running : given.a_job_whose_silo_went_away_while_stopping
{
    int _writesBefore;

    void Establish()
    {
        _job.CurrentState.Status = JobStatus.Running;
        StoredJobStep(_stepId).Status = JobStepStatus.Running;
        _writesBefore = Writes;
    }

    Task Because() => _job.FinalizeStop();

    [Fact] void should_not_run_the_stopped_hook() => _job.StoppedCount.ShouldEqual(0);
    [Fact] void should_leave_the_job_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_not_write_anything() => Writes.ShouldEqual(_writesBefore);
    [Fact] void should_not_notify_the_step() => _step.Verify(step => step.OnJobStopped(), Moq.Times.Never);
}
