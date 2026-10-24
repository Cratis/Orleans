// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_stops_while_removing;

public class and_its_current_run_succeeded_in_this_activation : given.a_job_being_removed_after_a_resume
{
    async Task Because()
    {
        await ReportSuccessFromCurrentRun();
        await ReportStopFromEarlierExecution();
    }

    [Fact] void should_count_the_success_once() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_not_count_the_stop() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(0);
    [Fact] void should_record_the_outcome_once() => _job.CurrentState.CompletedStepsCounted.ShouldContainOnly(_stepId);
    [Fact] void should_save_the_record_with_progress() => _job.WrittenStates[^1].CompletedStepsCounted.ShouldContainOnly(_stepId);
    [Fact] void should_handle_the_outcome_once() => _job.HandledStepResults.ShouldContainOnly(_stepId);
    [Fact] void should_not_finish_removal() => Clears.ShouldEqual(0);
}
