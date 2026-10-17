// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_succeeds;

public class without_a_generation : given.a_running_job_with_two_steps
{
    void Establish()
    {
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Stopped });
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Running });
    }

    Task Because() => _job.OnStepSucceeded(_firstId, JobStepResult.Succeeded());

    [Fact] void should_count_the_success() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_hand_the_success_to_the_job() => _job.HandledStepResults.ShouldContainOnly(_firstId);
}
