// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_running_in_stages;

public class and_an_incomplete_stop_resume_hook_succeeds_on_retry : for_Job.given.a_staged_job_left_running_after_a_rejected_incomplete_stop_resume
{
    async Task Because() => await _job.Resume();

    [Fact] void should_restart_the_stopped_step_in_the_current_stage() => StartsOf(_secondStepA).ShouldEqual(1);
    [Fact] void should_check_the_survivor_in_the_current_stage() => StartsOf(_secondStepB).ShouldEqual(1);
    [Fact] void should_not_restart_the_completed_stage() => StartsOf(_firstStep).ShouldEqual(0);
    [Fact] void should_not_cross_the_unfinished_stage_barrier() => StartsOf(_thirdStep).ShouldEqual(0);
    [Fact] void should_preserve_the_first_stages_success() => _job.CurrentState.Progress.Stages[0].SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_preserve_the_current_stages_total() => _job.CurrentState.Progress.Stages[1].TotalSteps.ShouldEqual(2);
    [Fact] void should_not_recount_current_stage_outcomes() => _job.CurrentState.Progress.Stages[1].SuccessfulSteps.ShouldEqual(0);
    [Fact] void should_preserve_the_execution_generation() => ((long)_job.CurrentState.StatusChanges.Count).ShouldEqual(_resumedGeneration);
}
