// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_succeeds;

public class and_storage_accounted_for_it_before_a_cold_activation : given.the_job
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

        // One step succeeded - its record is gone - and the job only heard about it after the activation counted.
        CountStepsInStorage((JobStepStatus.Running, 1));
        foreach (var status in new[] { JobStatus.PreparingJob, JobStatus.PreparingSteps, JobStatus.StartingSteps, JobStatus.Running })
        {
            _job.CurrentState.StatusChanges.Add(new() { Status = status });
        }
        _job.CurrentState.Status = JobStatus.Running;
        _job.CurrentState.Progress.TotalSteps = 2;
        await _job.OnActivateAsync(CancellationToken.None);
    }

    Task Because() => _job.OnStepSucceeded(_doneId, JobStepResult.Succeeded());

    [Fact] void should_count_the_step_storage_already_accounted_for_once() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_not_complete_the_job_while_a_step_is_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
