// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_finalizing_a_stop;

public class and_a_step_is_still_stopping : given.a_job_whose_silo_went_away_while_stopping
{
    void Establish() => StoredJobStep(_stepId).Status = JobStepStatus.Running;

    async Task Because()
    {
        await _job.FinalizeStop();
        await _job.FinalizeStop();
    }

    [Fact] void should_not_run_the_stopped_hook() => _job.StoppedCount.ShouldEqual(0);
    [Fact] void should_leave_the_job_stopped() => _job.CurrentState.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_tell_the_step_to_stop_once() => _step.Verify(step => step.OnJobStopped(), Moq.Times.Once);
    [Fact] void should_not_start_the_step() => _step.Verify(step => step.Start(Moq.It.IsAny<GrainId>()), Moq.Times.Never);
}
