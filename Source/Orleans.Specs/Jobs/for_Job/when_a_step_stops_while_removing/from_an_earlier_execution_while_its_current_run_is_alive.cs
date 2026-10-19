// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_stops_while_removing;

public class from_an_earlier_execution_while_its_current_run_is_alive : given.a_job_being_removed_after_a_resume
{
    async Task Because()
    {
        await ReportStopFromEarlierExecution();
        await ReportStopFromEarlierExecution();
    }

    [Fact] void should_tell_the_current_run_to_remove_once() => _currentRun.Received(1).OnJobRemoved();
    [Fact] void should_tell_it_in_the_current_execution() => _generationsTheCurrentRunWasToldToRemoveIn.ShouldContainOnly(CurrentGeneration);
    [Fact] void should_count_the_stop_once() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(1);
    [Fact] void should_not_finish_the_removal_before_the_other_step_reports() => Clears.ShouldEqual(0);
}
