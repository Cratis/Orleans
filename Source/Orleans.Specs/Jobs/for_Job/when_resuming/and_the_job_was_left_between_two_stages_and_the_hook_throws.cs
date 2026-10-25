// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_job_was_left_between_two_stages_and_the_hook_throws : when_running_in_stages.given.a_job_with_three_stages
{
    Result<ResumeJobSuccess, ResumeJobError> _result;

    async Task Because()
    {
        await _job.Start(new());
        _job.CurrentState.Progress.SuccessfulSteps = 1;
        _job.CurrentState.Progress.Stages[0].SuccessfulSteps = 1;
        _job.BeforeResuming = () => Task.FromException(new InvalidOperationException("Resume was rejected"));
        _result = await _job.Resume();
    }

    [Fact] void should_report_failure() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_start_the_first_step_of_the_next_stage() => ShouldNotHaveStarted(_secondStepA);
    [Fact] void should_not_start_the_second_step_of_the_next_stage() => ShouldNotHaveStarted(_secondStepB);
    [Fact] void should_be_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
