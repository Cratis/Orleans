// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_succeeds;

public class after_the_drained_job_resumed : given.a_running_job_with_two_steps
{
    const string GenerationKey = "Cratis.Orleans.Jobs.Generation";

    Result<JobError> _result;

    async Task Establish()
    {
        DrainsAs(_first, _firstId, JobStepStatus.CompletedSuccessfully, JobStepResult.Succeeded());
        DrainsAs(_second, _secondId, JobStepStatus.Stopped, Stopped);
        (await _job.StopAndDrain(TimeSpan.FromSeconds(5))).ShouldEqual(JobDrainStatus.Drained);
        _job.ShouldBeResumable = true;
        (await _job.Resume()).IsSuccess.ShouldBeTrue();
    }

    async Task Because()
    {
        // The success the drain already took, reported late by the step with the generation it ran in.
        RequestContext.Set(GenerationKey, 1L);
        try
        {
            _result = await _job.OnStepSucceeded(_firstId, JobStepResult.Succeeded());
        }
        finally
        {
            RequestContext.Remove(GenerationKey);
        }
    }

    [Fact] void should_acknowledge_the_report() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_count_the_success_once() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_hand_the_success_to_the_job_once() => _job.HandledStepResults.Count(_ => _ == _firstId).ShouldEqual(1);
    [Fact] void should_keep_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
