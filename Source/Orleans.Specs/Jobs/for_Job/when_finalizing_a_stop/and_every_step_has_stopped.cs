// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_finalizing_a_stop;

public class and_every_step_has_stopped : given.a_job_whose_silo_went_away_while_stopping
{
    Task Because() => _job.FinalizeStop();

    [Fact] void should_run_the_stopped_hook_once() => _job.StoppedCount.ShouldEqual(1);
    [Fact] void should_leave_the_job_stopped() => _job.CurrentState.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_count_the_step_as_stopped() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(1);
    [Fact] void should_persist_the_stopped_progress() => _job.WrittenStates[^1].Progress.IsStopped.ShouldBeTrue();
    [Fact] void should_record_the_stop_as_finalized() => _job.WrittenStates[^1].StopFinalized.ShouldBeTrue();
    [Fact] void should_let_the_job_deactivate() => Deactivations.ShouldEqual(1);
    [Fact] void should_not_complete_the_job() => _job.AllStepsCompletedCount.ShouldEqual(0);
    [Fact] void should_not_notify_the_step() => _step.Verify(step => step.OnJobStopped(), Moq.Times.Never);
    [Fact] void should_not_start_the_step() => _step.Verify(step => step.Start(Moq.It.IsAny<GrainId>()), Moq.Times.Never);
}
