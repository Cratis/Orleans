// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_succeeds;

/// <summary>
/// A job without stages was resumed by an earlier activation. One step persisted its success - which removes its
/// record - before this activation loaded the steps, so it does not track the step when the success arrives.
/// </summary>
public class of_an_earlier_execution_after_a_cold_activation : given.the_job
{
    const string ReportedKey = "Cratis.Orleans.Jobs.ReportedGeneration";

    JobStepId _lateId;
    JobStepId _runningId;

    async Task Establish()
    {
        _lateId = JobStepId.New();
        _runningId = JobStepId.New();
        AddJobStep(_lateId);
        AddJobStep(_runningId);
        StoredJobStep(_runningId).Status = JobStepStatus.Running;
        foreach (var status in new[] { JobStatus.PreparingJob, JobStatus.PreparingSteps, JobStatus.StartingSteps, JobStatus.Running, JobStatus.Stopped, JobStatus.Running })
        {
            _job.CurrentState.StatusChanges.Add(new() { Status = status });
        }
        _job.CurrentState.Status = JobStatus.Running;
        _job.CurrentState.Progress.TotalSteps = 2;
        await _job.OnActivateAsync(CancellationToken.None);
    }

    async Task Because()
    {
        await Report(_lateId, 4);
        _storedJobStepStates.Remove(StoredJobStep(_runningId));
        await Report(_runningId, 6);
    }

    [Fact] void should_complete_the_job() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedSuccessfully);
    [Fact] void should_complete_once() => _job.AllStepsCompletedCount.ShouldEqual(1);
    [Fact] void should_count_every_step_left_in_storage_once() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(_job.CurrentState.Progress.TotalSteps);

    async Task Report(JobStepId stepId, long generation)
    {
        RequestContext.Set(ReportedKey, generation);
        try
        {
            await _job.OnStepSucceeded(stepId, JobStepResult.Succeeded());
        }
        finally
        {
            RequestContext.Remove(ReportedKey);
        }
    }
}
