// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_stops_while_removing;

public class from_an_earlier_execution_that_finishes_the_removal : given.a_job_being_removed_after_a_resume
{
    Task Establish() => _job.OnStepStopped(_secondStepId, Stopped);

    Task Because() => ReportStopFromEarlierExecution();

    [Fact] void should_finish_the_removal() => Clears.ShouldEqual(1);
    [Fact] void should_tell_the_current_run_to_remove_once() => _currentRun.Received(1).OnJobRemoved();
    [Fact] void should_tell_it_in_the_execution_that_was_removed() => _generationsTheCurrentRunWasToldToRemoveIn.ShouldContainOnly(CurrentGeneration);
}
