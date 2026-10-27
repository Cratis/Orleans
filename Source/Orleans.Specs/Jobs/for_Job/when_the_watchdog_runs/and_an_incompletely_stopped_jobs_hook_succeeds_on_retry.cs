// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_the_watchdog_runs;

public class and_an_incompletely_stopped_jobs_hook_succeeds_on_retry : for_Job.given.a_job_left_running_after_an_incomplete_stop_and_a_rejected_resume
{
    void Establish()
    {
        CountStepsInStorage((JobStepStatus.Stopped, 1), (JobStepStatus.Running, 1));
        AllowResume();
    }

    async Task Because()
    {
        await _job.RunWatchdog();
        await _job.RunWatchdog();
        await _job.RunWatchdog();
    }

    [Fact] void should_recover_the_previously_untracked_stopped_step_once() => StartsOf(0).ShouldEqual(1);
    [Fact] void should_check_the_surviving_step_once() => StartsOf(1).ShouldEqual(1);
    [Fact] void should_run_the_resume_hook_only_once_more() => _job.BeforeResumingCount.ShouldEqual(2);
    [Fact] void should_run_the_hooks_before_starting_steps() => HooksPrecedeStarts().ShouldBeTrue();
    [Fact] void should_carry_the_existing_running_generation() => GenerationOfStart(0).ShouldEqual(_resumedGeneration);
    [Fact] void should_keep_the_generation() => ((long)_job.CurrentState.StatusChanges.Count).ShouldEqual(_resumedGeneration);
    [Fact] void should_keep_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_leave_the_recovered_step_running_in_storage() => StoredJobStep(_stepIds[0]).Status.ShouldEqual(JobStepStatus.Running);
}
