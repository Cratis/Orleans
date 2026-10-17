// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_stopped_jobs_hook_throws : given.a_stopped_job_rejecting_resume
{
    JobStatus _statusSeenByHook;
    JobStatus _persistedStatusSeenByHook;

    void Establish() => _job.BeforeResuming = () =>
    {
        _statusSeenByHook = _job.CurrentState.Status;
        _persistedStatusSeenByHook = _job.WrittenStates[^1].Status;
        return Task.FromException(new InvalidOperationException("Resume was rejected"));
    };

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_report_failure() => ((CannotResumeJobError)_result.AsT1).ShouldEqual(CannotResumeJobError.Unknown);
    [Fact] void should_show_running_to_the_hook() => _statusSeenByHook.ShouldEqual(JobStatus.Running);
    [Fact] void should_persist_running_before_the_hook() => _persistedStatusSeenByHook.ShouldEqual(JobStatus.Running);
    [Fact] void should_return_to_stopped() => _job.CurrentState.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_restore_stopped_progress() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(1);
    [Fact] void should_append_the_compensation_to_history() => _job.CurrentState.StatusChanges.Skip(_changesBefore).Select(change => change.Status).ShouldEqual([JobStatus.Running, JobStatus.Stopped]);
    [Fact] void should_persist_the_compensating_stop() => _job.WrittenStates[^1].Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_persist_the_restored_progress() => _job.WrittenStates[^1].Progress.StoppedSteps.ShouldEqual(1);
    [Fact] void should_write_the_resume_and_compensation() => Writes.ShouldEqual(_writesBefore + 2);
    [Fact] void should_not_start_steps() => _step.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_not_drain_steps() => _step.Verify(step => step.Drain(It.IsAny<long>(), It.IsAny<TimeSpan>()), Times.Never);
    [Fact] void should_not_run_the_stop_hook() => _job.StoppedCount.ShouldEqual(0);
    [Fact] void should_not_complete_the_job() => _job.AllStepsCompletedCount.ShouldEqual(0);
}
