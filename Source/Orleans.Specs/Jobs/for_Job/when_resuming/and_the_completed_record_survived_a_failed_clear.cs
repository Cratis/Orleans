// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_completed_record_survived_a_failed_clear : given.a_job_ready_to_complete
{
    Result<ResumeJobSuccess, ResumeJobError> _result;

    async Task Establish()
    {
        _failClears = true;
        await _job.Resume();
        await _silo.TimerRegistry.FireAllAsync();
        await ActivateFromDurableState();
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_report_completion() => _result.AsT0.ShouldEqual(ResumeJobSuccess.JobIsCompleted);
    [Fact] void should_preserve_the_terminal_status() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedSuccessfully);
    [Fact] void should_not_repeat_the_completion_hook_after_reactivation() => _job.AllStepsCompletedCount.ShouldEqual(0);
    [Fact] void should_not_plan_steps_again() => _job.PrepareStepsCount.ShouldEqual(0);
}
