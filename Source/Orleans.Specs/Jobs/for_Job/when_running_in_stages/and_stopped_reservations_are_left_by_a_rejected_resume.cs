// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_running_in_stages;

public class and_stopped_reservations_are_left_by_a_rejected_resume : for_Job.given.a_staged_job_left_running_after_a_rejected_incomplete_stop_resume
{
    int _stoppedCandidatesStartedBeforeRoomWasFreed;
    int _survivorStartsBeforeRoomWasFreed;
    int _laterStageStartsBeforeRoomWasFreed;

    protected override bool HasAdditionalStoppedCandidate => true;

    async Task Because()
    {
        await _job.Resume();
        _stoppedCandidatesStartedBeforeRoomWasFreed = StartsOf(_secondStepA) + StartsOf(_additionalStep);
        _survivorStartsBeforeRoomWasFreed = StartsOf(_secondStepB);
        _laterStageStartsBeforeRoomWasFreed = StartsOf(_thirdStep);
        _storedJobStepStates.Remove(StoredJobStep(_secondB));
        await Succeed(_secondB);
    }

    [Fact] void should_use_only_one_free_slot_for_stopped_candidates() => _stoppedCandidatesStartedBeforeRoomWasFreed.ShouldEqual(1);
    [Fact] void should_preserve_the_running_survivors_reservation() => _survivorStartsBeforeRoomWasFreed.ShouldEqual(1);
    [Fact] void should_start_the_other_candidate_once_a_slot_is_freed() => (StartsOf(_secondStepA) + StartsOf(_additionalStep)).ShouldEqual(2);
    [Fact] void should_not_open_the_later_stage_during_recovery() => _laterStageStartsBeforeRoomWasFreed.ShouldEqual(0);
    [Fact] void should_keep_the_later_stage_behind_its_barrier() => StartsOf(_thirdStep).ShouldEqual(0);
    [Fact] void should_not_restart_the_completed_stage() => StartsOf(_firstStep).ShouldEqual(0);
    [Fact] void should_count_the_survivors_success_once() => _job.CurrentState.Progress.Stages[1].SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_keep_the_generation() => ((long)_job.CurrentState.StatusChanges.Count).ShouldEqual(_resumedGeneration);
}
