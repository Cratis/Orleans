// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_stops_while_removing;

public class and_its_replacement_success_is_saved_before_reactivation : given.a_job_being_removed_after_a_resume
{
    int _clearsBeforeSecondStop;
    given.SomeJobState _savedState;

    async Task Establish()
    {
        await ReportStopFromEarlierExecution();
        await ReportSuccessFromCurrentRun();
        _savedState = _job.WrittenStates[^1];
        _savedState.StoppedStepsCounted.ShouldBeEmpty();
        _storedJobStepStates.Remove(StoredJobStep(_stepId));
        await ReactivateFrom(_savedState);
    }

    async Task Because()
    {
        await ReportStopFromEarlierExecution();
        _clearsBeforeSecondStop = Clears;
        await _job.OnStepStopped(_secondStepId, Stopped);
    }

    [Fact] void should_not_finish_before_the_other_step_stops() => _clearsBeforeSecondStop.ShouldEqual(0);
    [Fact] void should_finish_after_the_other_step_stops() => Clears.ShouldEqual(1);
    [Fact] void should_keep_the_success() => _job.SuccessfulStepsAtCompletion.ShouldEqual(1);
    [Fact] void should_count_only_the_other_stop() => _job.StoppedStepsAtCompletion.ShouldEqual(1);
    [Fact] void should_not_handle_the_replayed_outcome() => _job.HandledStepResults.ShouldContainOnly(_secondStepId);
    [Fact] void should_not_mutate_the_previous_activations_saved_progress() => _savedState.Progress.StoppedSteps.ShouldEqual(0);
    [Fact] void should_not_mutate_the_previous_activations_saved_stops() => _savedState.StoppedStepsCounted.ShouldBeEmpty();
    [Fact] void should_not_share_the_previous_activations_record() => ReferenceEquals(_job.CompletedStepsCountedAtCompletion, _savedState.CompletedStepsCounted).ShouldBeFalse();
}
