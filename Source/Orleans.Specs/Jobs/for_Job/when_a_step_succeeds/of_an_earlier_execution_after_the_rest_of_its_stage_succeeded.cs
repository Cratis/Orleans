// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_succeeds;

/// <summary>
/// A step persisted its success - which removes its record - while the job was stopped. After a resume reloaded the
/// stage without it, the other step of the stage succeeded too, so no record of the stage is left when the late
/// success arrives.
/// </summary>
public class of_an_earlier_execution_after_the_rest_of_its_stage_succeeded : when_running_in_stages.given.a_job_with_three_stages
{
    const string ReportedKey = "Cratis.Orleans.Jobs.ReportedGeneration";

    async Task Establish()
    {
        _job.ShouldBeResumable = true;
        await _job.Start(new());
        _storedJobStepStates.Remove(StoredJobStep(_first));
        await Succeed(_first);

        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Stopped });
        _job.CurrentState.Status = JobStatus.Stopped;
        StoredJobStep(_secondB).Status = JobStepStatus.Stopped;
        _storedJobStepStates.Remove(StoredJobStep(_secondA));
        (await _job.Resume()).IsSuccess.ShouldBeTrue();
        _storedJobStepStates.Remove(StoredJobStep(_secondB));
        await Succeed(_secondB);
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

        _storedJobStepStates.Remove(StoredJobStep(_third));
        await Succeed(_third);
    }

    [Fact] void should_start_the_stage_after_the_one_the_late_success_completed() => ShouldHaveStarted(_thirdStep);
    [Fact] void should_complete_the_job() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedSuccessfully);
    [Fact] void should_complete_once() => _job.AllStepsCompletedCount.ShouldEqual(1);
}
