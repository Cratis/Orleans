// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

/// <summary>
/// The failure of the last step in a barrier stage is persisted before the job stops there, so a silo that stops in
/// between leaves a job that is running with a failed barrier nothing acted on - it must not carry on past it.
/// </summary>
public class and_the_job_was_left_at_a_failed_barrier : when_running_in_stages.given.a_job_with_three_stages
{
    Result<ResumeJobSuccess, ResumeJobError> _result;

    async Task Because()
    {
        await _job.Start(new());
        _job.CurrentState.Progress.FailedSteps = 1;
        _job.CurrentState.Progress.Stages[0].FailedSteps = 1;
        _result = await _job.Resume();
    }

    [Fact] void should_report_the_job_as_completed() => ((ResumeJobSuccess)_result).ShouldEqual(ResumeJobSuccess.JobIsCompleted);
    [Fact] void should_not_start_the_next_stage() => ShouldNotHaveStarted(_secondStepA);
    [Fact] void should_count_the_steps_behind_it_as_unreachable() => _job.CurrentState.Progress.UnreachableSteps.ShouldEqual(3);
    [Fact] void should_complete_the_job_with_failures() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedWithFailures);
}
