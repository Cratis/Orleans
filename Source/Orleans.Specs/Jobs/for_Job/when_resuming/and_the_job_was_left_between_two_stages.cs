// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

/// <summary>
/// The outcome of the last step in a stage is persisted before the next stage starts, so a silo that stops in
/// between leaves a job that is running with nothing running.
/// </summary>
public class and_the_job_was_left_between_two_stages : when_running_in_stages.given.a_job_with_three_stages
{
    Result<ResumeJobSuccess, ResumeJobError> _result;

    async Task Because()
    {
        await _job.Start(new());
        _job.CurrentState.Progress.SuccessfulSteps = 1;
        _job.CurrentState.Progress.Stages[0].SuccessfulSteps = 1;
        _result = await _job.Resume();
    }

    [Fact] void should_report_the_job_as_resumed() => ((ResumeJobSuccess)_result).ShouldEqual(ResumeJobSuccess.Success);
    [Fact] void should_start_the_first_step_of_the_next_stage() => ShouldHaveStarted(_secondStepA);
    [Fact] void should_start_the_second_step_of_the_next_stage() => ShouldHaveStarted(_secondStepB);
    [Fact] void should_not_start_the_stage_after_it() => ShouldNotHaveStarted(_thirdStep);
    [Fact] void should_be_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
