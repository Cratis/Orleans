// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_interrupted_jobs_hook_throws : given.an_interrupted_job_rejecting_resume
{
    int _changesBefore;

    void Establish() => _changesBefore = _job.CurrentState.StatusChanges.Count;

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_report_failure() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_preserve_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_not_change_history() => _job.CurrentState.StatusChanges.Count.ShouldEqual(_changesBefore);
    [Fact] void should_preserve_stopped_progress() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(1);
    [Fact] void should_not_drain_surviving_work() => _unfinished.Verify(step => step.Drain(It.IsAny<long>(), It.IsAny<TimeSpan>()), Times.Never);
    [Fact] void should_not_launch_work() => _unfinished.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_not_claim_a_pending_drain() => _job.CurrentState.DrainPending.ShouldBeFalse();
}
