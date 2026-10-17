// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_succeeds;

/// <summary>
/// After a resume reloaded a stage, a step it tracks persisted its success - which removes its record - and its report
/// waits behind a late success of an earlier execution from a step the job does not track. Both count once, and the
/// job completes only after the stage that follows has reported too.
/// </summary>
public class of_an_earlier_execution_while_a_tracked_step_has_persisted_its_success : when_running_in_stages.given.a_job_with_three_stages
{
    const string ReportedKey = "Cratis.Orleans.Jobs.ReportedGeneration";

    JobStatus _statusBeforeTheLastStep;
    int _successfulBeforeTheLastStep;

    async Task Establish()
    {
        _job.ShouldBeResumable = true;
        await _job.Start(new());
        _storedJobStepStates.Remove(StoredJobStep(_first));
        await Succeed(_first);

        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Stopped });
        _job.CurrentState.Status = JobStatus.Stopped;
        _storedJobStepStates.Remove(StoredJobStep(_secondA));
        StoredJobStep(_secondB).Status = JobStepStatus.Stopped;
        (await _job.Resume()).IsSuccess.ShouldBeTrue();
        StoredJobStep(_secondB).Status = JobStepStatus.Running;
        _storedJobStepStates.Remove(StoredJobStep(_secondB));
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

        await Succeed(_secondB);
        _statusBeforeTheLastStep = _job.CurrentState.Status;
        _successfulBeforeTheLastStep = _job.CurrentState.Progress.SuccessfulSteps;
        _storedJobStepStates.Remove(StoredJobStep(_third));
        await Succeed(_third);
    }

    [Fact] void should_start_the_stage_after_both_of_the_stage_reported() => ShouldHaveStarted(_thirdStep);
    [Fact] void should_still_be_running_before_the_last_step_reports() => _statusBeforeTheLastStep.ShouldEqual(JobStatus.Running);
    [Fact] void should_have_counted_everything_but_the_last_step_before_it_reports() => _successfulBeforeTheLastStep.ShouldEqual(_job.CurrentState.Progress.TotalSteps - 1);
    [Fact] void should_complete_the_job() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedSuccessfully);
    [Fact] void should_complete_once() => _job.AllStepsCompletedCount.ShouldEqual(1);
    [Fact] void should_count_every_step_once() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(_job.CurrentState.Progress.TotalSteps);
}
