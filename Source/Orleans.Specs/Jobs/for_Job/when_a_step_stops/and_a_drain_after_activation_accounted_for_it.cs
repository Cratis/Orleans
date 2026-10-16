// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_stops;

public class and_a_drain_after_activation_accounted_for_it : given.a_job_activated_with_a_pending_drain
{
    const string GenerationKey = "Cratis.Orleans.Jobs.ReportedGeneration";

    Result<JobError> _result;

    Task Establish() => _silo.TimerRegistry.FireAllAsync();

    async Task Because()
    {
        RequestContext.Set(GenerationKey, 1L);
        try
        {
            _result = await _job.OnStepStopped(_runningId, Stopped);
        }
        finally
        {
            RequestContext.Remove(GenerationKey);
        }
    }

    [Fact] void should_acknowledge_the_report() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_count_the_stop_again() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(2);
    [Fact] void should_hand_the_outcome_to_the_job_once() => _job.HandledStepResults.Count(_ => _ == _runningId).ShouldEqual(1);
}
