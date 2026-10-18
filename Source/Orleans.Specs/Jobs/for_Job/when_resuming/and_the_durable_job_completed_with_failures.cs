// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_durable_job_completed_with_failures : given.a_job_ready_to_complete
{
    Result<ResumeJobSuccess, ResumeJobError> _result;

    async Task Establish()
    {
        _job.CurrentState.Progress.SuccessfulSteps = 0;
        _job.CurrentState.Progress.FailedSteps = 1;
        await _job.Resume();
        await ActivateFromDurableState();
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_report_completion() => _result.AsT0.ShouldEqual(ResumeJobSuccess.JobIsCompleted);
    [Fact] void should_preserve_completion_with_failures() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedWithFailures);
    [Fact] void should_not_repeat_the_completion_hook_after_reactivation() => _job.AllStepsCompletedCount.ShouldEqual(0);
    [Fact] void should_not_plan_steps_again() => _job.PrepareStepsCount.ShouldEqual(0);
}
