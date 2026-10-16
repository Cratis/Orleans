// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_succeeds;

public class with_an_older_generation : given.a_running_job_with_two_steps
{
    const string GenerationKey = "Cratis.Orleans.Jobs.Generation";

    void Establish()
    {
        // The job was stopped and resumed since the step was started.
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Stopped });
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Running });
    }

    async Task Because()
    {
        RequestContext.Set(GenerationKey, 1L);
        try
        {
            await _job.OnStepSucceeded(_firstId, JobStepResult.Succeeded());
            RequestContext.Set(GenerationKey, 1L);
            await _job.OnStepSucceeded(_firstId, JobStepResult.Succeeded());
        }
        finally
        {
            RequestContext.Remove(GenerationKey);
        }
    }

    [Fact] void should_count_the_success_once() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_hand_the_success_to_the_job_once() => _job.HandledStepResults.ShouldContainOnly(_firstId);
    [Fact] void should_keep_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
