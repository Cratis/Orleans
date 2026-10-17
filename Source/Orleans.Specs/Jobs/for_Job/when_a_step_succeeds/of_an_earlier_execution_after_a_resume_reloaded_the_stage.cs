// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_succeeds;

/// <summary>
/// A step persisted its success - which removes its record - while the job was stopped, and its report reached the
/// job only after a resume had reloaded the stage from step storage, without that step.
/// </summary>
public class of_an_earlier_execution_after_a_resume_reloaded_the_stage : when_running_in_stages.given.a_job_with_three_stages
{
    const string ReportedKey = "Cratis.Orleans.Jobs.ReportedGeneration";

    async Task Establish()
    {
        _job.ShouldBeResumable = true;
        await _job.Start(new());
        _storedJobStepStates.Remove(StoredJobStep(_first));
        await Succeed(_first);

        // Stopped while the second stage runs: one of its steps persists its stop, the other its success.
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Stopped });
        _job.CurrentState.Status = JobStatus.Stopped;
        StoredJobStep(_secondB).Status = JobStepStatus.Stopped;
        _storedJobStepStates.Remove(StoredJobStep(_secondA));
        (await _job.Resume()).IsSuccess.ShouldBeTrue();
        StoredJobStep(_secondB).Status = JobStepStatus.Running;
    }

    async Task Because()
    {
        RequestContext.Set(ReportedKey, 1L);
        try
        {
            await _job.OnStepSucceeded(_secondA, JobStepResult.Succeeded());
        }
        finally
        {
            RequestContext.Remove(ReportedKey);
        }

        _storedJobStepStates.Remove(StoredJobStep(_secondB));
        await Succeed(_secondB);
        _storedJobStepStates.Remove(StoredJobStep(_third));
        await Succeed(_third);
    }

    [Fact] void should_start_the_stage_after_the_one_the_late_success_completed() => ShouldHaveStarted(_thirdStep);
    [Fact] void should_complete_the_job() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedSuccessfully);
    [Fact] void should_complete_once() => _job.AllStepsCompletedCount.ShouldEqual(1);
    [Fact] void should_count_every_step_left_in_storage_once() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(_job.CurrentState.Progress.TotalSteps);
}
