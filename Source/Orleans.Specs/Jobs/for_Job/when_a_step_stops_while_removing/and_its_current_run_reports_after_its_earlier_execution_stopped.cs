// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_stops_while_removing;

public class and_its_current_run_reports_after_its_earlier_execution_stopped : given.a_job_being_removed_after_a_resume
{
    async Task Because()
    {
        await ReportStopFromEarlierExecution();
        await ReportStopFromCurrentRun();
        await _job.OnStepStopped(_secondStepId, Stopped);
    }

    [Fact] void should_tell_the_current_run_to_remove_once() => _currentRun.Received(1).OnJobRemoved();
    [Fact] void should_hand_each_stop_to_the_job_once() => _job.HandledStepResults.ShouldContainOnly(_stepId, _secondStepId);
    [Fact] void should_run_the_stopped_hook_once() => _job.StoppedCount.ShouldEqual(1);
    [Fact] void should_clear_the_job_once() => Clears.ShouldEqual(1);
}
