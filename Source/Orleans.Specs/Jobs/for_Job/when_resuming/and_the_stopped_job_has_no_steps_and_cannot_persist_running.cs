// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_stopped_job_has_no_steps_and_cannot_persist_running : given.a_job_ready_to_complete
{
    Result<ResumeJobSuccess, ResumeJobError> _result;

    void Establish()
    {
        _job.ShouldBeResumable = true;
        _job.CurrentState.Status = JobStatus.Stopped;
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.StartingSteps, Occurred = DateTimeOffset.UtcNow });
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Stopped, Occurred = DateTimeOffset.UtcNow });
        _storedStatus = JobStatus.Stopped;
        _failWrites = true;
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_report_the_failed_resume() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_restore_the_stopped_status() => _job.CurrentState.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_leave_storage_stopped() => _storedStatus.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_not_run_the_completion_hook() => _job.AllStepsCompletedCount.ShouldEqual(0);
    [Fact] void should_not_clear_state() => _clearAttempts.ShouldEqual(0);
    [Fact] void should_attempt_the_running_write_once() => _writeAttempts.ShouldEqual(1);
}
