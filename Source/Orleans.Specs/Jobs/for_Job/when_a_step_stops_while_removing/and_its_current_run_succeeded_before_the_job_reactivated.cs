// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_stops_while_removing;

public class and_its_current_run_succeeded_before_the_job_reactivated : given.a_job_being_removed_after_a_resume
{
    Result<JobError> _result;

    void Establish()
    {
        _job.CurrentState.CompletedStepsCounted = [_stepId];
        _job.CurrentState.Progress.SuccessfulSteps = 1;
        _storedJobStepStates.Remove(StoredJobStep(_stepId));
    }

    async Task Because() => _result = await ReportStopFromEarlierExecution();

    [Fact] void should_acknowledge_the_report() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_count_the_stop() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(0);
    [Fact] void should_not_finish_removal() => Clears.ShouldEqual(0);
    [Fact] void should_not_handle_the_outcome_again() => _job.HandledStepResults.ShouldBeEmpty();
}
