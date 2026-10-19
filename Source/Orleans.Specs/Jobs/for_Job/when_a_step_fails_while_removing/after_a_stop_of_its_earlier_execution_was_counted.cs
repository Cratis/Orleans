// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_fails_while_removing;

public class after_a_stop_of_its_earlier_execution_was_counted : given.a_job_being_removed_after_a_resume
{
    async Task Because()
    {
        await ReportStopFromEarlierExecution();
        await ReportFailureFromCurrentRun();
        await _job.OnStepStopped(_secondStepId, Stopped);
    }

    [Fact] void should_finish_the_removal_only_once_every_step_reported() => _job.ResultsSeenAtCompletion.ShouldEqual(3);
    [Fact] void should_run_the_stopped_hook_once() => _job.StoppedCount.ShouldEqual(1);
    [Fact] void should_clear_the_job_once() => Clears.ShouldEqual(1);
}
