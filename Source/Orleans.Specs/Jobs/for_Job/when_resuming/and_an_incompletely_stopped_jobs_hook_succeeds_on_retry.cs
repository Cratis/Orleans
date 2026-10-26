// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_an_incompletely_stopped_jobs_hook_succeeds_on_retry : given.a_job_left_running_after_an_incomplete_stop_and_a_rejected_resume
{
    void Establish() => AllowResume();

    async Task Because() => await _job.Resume();

    [Fact] void should_recover_the_previously_untracked_stopped_step() => StartsOf(0).ShouldEqual(1);
    [Fact] void should_check_the_surviving_step_idempotently() => StartsOf(1).ShouldEqual(1);
    [Fact] void should_run_the_hooks_before_starting_steps() => HooksPrecedeStarts().ShouldBeTrue();
    [Fact] void should_carry_the_existing_running_generation() => GenerationOfStart(0).ShouldEqual(_resumedGeneration);
    [Fact] void should_keep_the_generation() => ((long)_job.CurrentState.StatusChanges.Count).ShouldEqual(_resumedGeneration);
    [Fact] void should_not_write_another_transition() => Writes.ShouldEqual(_writesAfterRejectedResume);
    [Fact] void should_keep_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_have_rejected_the_first_resume() => _rejectedResumeFailed.ShouldBeTrue();
    [Fact] void should_not_have_compensated_the_incomplete_stop() => _stoppedWritesAfterRejectedResume.ShouldEqual(_stoppedWritesBeforeResume);
    [Fact] void should_count_no_stops_in_the_new_execution() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(0);
}
