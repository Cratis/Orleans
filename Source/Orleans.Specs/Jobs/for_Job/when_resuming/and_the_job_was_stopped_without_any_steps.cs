// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

/// <summary>
/// A job that prepared no steps at all and was stopped before it could conclude has nothing to resume either - it
/// concludes, rather than staying stopped behind a reported success (Cratis/Chronicle#4363).
/// </summary>
public class and_the_job_was_stopped_without_any_steps : given.the_job
{
    Result<ResumeJobSuccess, ResumeJobError> _result;

    void Establish() => _job.ShouldBeResumable = true;

    async Task Because()
    {
        await _job.Start(new());

        // The history the stuck catch-up jobs in production carried: prepared, started, running, stopped.
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.StartingSteps, Occurred = DateTimeOffset.UtcNow });
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Running, Occurred = DateTimeOffset.UtcNow });
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Stopped, Occurred = DateTimeOffset.UtcNow });
        _job.CurrentState.Status = JobStatus.Stopped;
        _job.AllStepsCompletedCount = 0;

        _result = await _job.Resume();
    }

    [Fact] void should_report_the_job_as_completed() => ((ResumeJobSuccess)_result).ShouldEqual(ResumeJobSuccess.JobIsCompleted);
    [Fact] void should_complete_the_job() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedSuccessfully);
    [Fact] void should_run_the_completion() => _job.AllStepsCompletedCount.ShouldEqual(1);
}
