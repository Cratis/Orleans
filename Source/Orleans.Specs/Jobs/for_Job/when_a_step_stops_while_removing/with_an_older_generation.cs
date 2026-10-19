// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_stops_while_removing;

public class with_an_older_generation : given.a_job_being_removed_with_two_steps
{
    const string GenerationKey = "Cratis.Orleans.Jobs.ReportedGeneration";

    Result<JobError> _first;
    Result<JobError> _again;

    async Task Because()
    {
        _first = await ReportFromEarlierExecution();
        _again = await ReportFromEarlierExecution();
    }

    [Fact] void should_acknowledge_the_report() => _first.IsSuccess.ShouldBeTrue();
    [Fact] void should_acknowledge_it_again() => _again.IsSuccess.ShouldBeTrue();
    [Fact] void should_count_the_stop_once() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(1);
    [Fact] void should_hand_the_stop_to_the_job_once() => _job.HandledStepResults.ShouldContainOnly(_stepId);

    async Task<Result<JobError>> ReportFromEarlierExecution()
    {
        RequestContext.Set(GenerationKey, 1L);
        try
        {
            return await _job.OnStepStopped(_stepId, Stopped);
        }
        finally
        {
            RequestContext.Remove(GenerationKey);
        }
    }
}
