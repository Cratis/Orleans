// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Job.when_draining;

public class and_a_push_is_queued_behind_the_drain : given.a_running_job_with_two_steps
{
    const string GenerationKey = "Cratis.Orleans.Jobs.ReportedGeneration";

    Result<JobError> _push;

    async Task Establish()
    {
        DrainsAs(_first, _firstId, JobStepStatus.CompletedSuccessfully, JobStepResult.Succeeded());
        DrainsAs(_second, _secondId, JobStepStatus.Stopped, Stopped);
        await _job.StopAndDrain(TimeSpan.FromSeconds(5));
    }

    async Task Because()
    {
        // The step's own report of the outcome the drain took, sent with the generation it ran in.
        RequestContext.Set(GenerationKey, 1L);
        try
        {
            _push = await _job.OnStepSucceeded(_firstId, JobStepResult.Succeeded());
        }
        finally
        {
            RequestContext.Remove(GenerationKey);
        }
    }

    [Fact] void should_acknowledge_the_report() => _push.IsSuccess.ShouldBeTrue();
    [Fact] void should_count_the_step_once() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_hand_the_outcome_to_the_job_once() => _job.HandledStepResults.Count(_ => _ == _firstId).ShouldEqual(1);
}
