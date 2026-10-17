// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_succeeds;

public class and_a_step_tracked_at_activation_reports : given.the_job
{
    JobStepId _doneId;
    JobStepId _runningId;

    async Task Establish()
    {
        _doneId = JobStepId.New();
        _runningId = JobStepId.New();
        AddJobStep(_doneId);
        AddJobStep(_runningId);
        StoredJobStep(_runningId).Status = JobStepStatus.Running;
        CountStepsInStorage((JobStepStatus.Running, 1));
        foreach (var status in new[] { JobStatus.PreparingJob, JobStatus.PreparingSteps, JobStatus.StartingSteps, JobStatus.Running })
        {
            _job.CurrentState.StatusChanges.Add(new() { Status = status });
        }
        _job.CurrentState.Status = JobStatus.Running;
        _job.CurrentState.Progress.TotalSteps = 2;
        await _job.OnActivateAsync(CancellationToken.None);
        await _job.OnStepSucceeded(_doneId, JobStepResult.Succeeded());
    }

    async Task Because()
    {
        _storedJobStepStates.Remove(StoredJobStep(_runningId));
        await _job.OnStepSucceeded(_runningId, JobStepResult.Succeeded());
    }

    [Fact] void should_count_it() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(2);
    [Fact] void should_complete_the_job() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedSuccessfully);
}
