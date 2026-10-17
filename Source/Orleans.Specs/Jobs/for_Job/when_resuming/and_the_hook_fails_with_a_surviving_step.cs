// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_hook_fails_with_a_surviving_step : given.a_stopped_job_rejecting_resume
{
    void Establish() => _job.CurrentState.Progress.StoppedSteps = 0;

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_report_failure() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_compensate_an_incomplete_stop() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_not_restore_an_incomplete_stop_to_progress() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(0);
    [Fact] void should_not_write_a_compensating_stop() => Writes.ShouldEqual(_writesBefore + 1);
    [Fact] void should_not_start_steps() => _step.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_not_drain_the_survivor() => _step.Verify(step => step.Drain(It.IsAny<long>(), It.IsAny<TimeSpan>()), Times.Never);
}
