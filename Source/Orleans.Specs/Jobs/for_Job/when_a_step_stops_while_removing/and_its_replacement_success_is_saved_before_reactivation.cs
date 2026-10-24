// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_stops_while_removing;

public class and_its_replacement_success_is_saved_before_reactivation : given.a_job_being_removed_after_a_resume
{
    int _clearsBeforeSecondStop;

    async Task Establish()
    {
        await ReportStopFromEarlierExecution();
        await ReportSuccessFromCurrentRun();
        _job.WrittenStates[^1].StoppedStepsCounted.ShouldBeEmpty();
        await ReactivateFrom(_job.WrittenStates[^1]);
    }

    async Task Because()
    {
        await ReportStopFromEarlierExecution();
        _clearsBeforeSecondStop = Clears;
        await _job.OnStepStopped(_secondStepId, Stopped);
    }

    [Fact] void should_not_finish_before_the_other_step_stops() => _clearsBeforeSecondStop.ShouldEqual(0);
    [Fact] void should_finish_after_the_other_step_stops() => Clears.ShouldEqual(1);
    [Fact] void should_keep_the_success() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_count_only_the_other_stop() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(1);
    [Fact] void should_not_handle_the_replayed_outcome() => _job.HandledStepResults.ShouldContainOnly(_secondStepId);
}
