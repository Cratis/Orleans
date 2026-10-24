// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_stops_while_removing;

public class and_its_current_failure_was_saved_before_reactivation : given.a_job_being_removed_after_a_resume
{
    async Task Establish()
    {
        StoredJobStep(_stepId).Status = JobStepStatus.Failed;
        await ReportFailureFromCurrentRun();
        await ReactivateFrom(_job.WrittenStates[^1]);
    }

    async Task Because() => await ReportStopFromEarlierExecution();

    [Fact] void should_keep_the_failure() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(1);
    [Fact] void should_not_count_the_stop() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(0);
    [Fact] void should_keep_the_recorded_failure() => _job.CurrentState.CompletedStepsCounted.ShouldContainOnly(_stepId);
    [Fact] void should_not_handle_the_outcome_again() => _job.HandledStepResults.ShouldBeEmpty();
    [Fact] void should_not_finish_removal() => Clears.ShouldEqual(0);
}
