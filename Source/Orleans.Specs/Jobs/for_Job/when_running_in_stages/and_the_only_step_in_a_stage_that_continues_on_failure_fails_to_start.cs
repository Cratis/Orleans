// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_running_in_stages;

public class and_the_only_step_in_a_stage_that_continues_on_failure_fails_to_start : given.a_job_with_three_stages
{
    Result<StartJobError> _result;

    void Establish()
    {
        _job.StagesContinuingOnFailure.Add(0);
        _firstStep.Setup(_ => _.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Failed(StartJobStepError.NotPrepared));
    }

    async Task Because() => _result = await _job.Start(new());

    [Fact] void should_report_that_the_first_stage_failed_starting() => ((StartJobError)_result).ShouldEqual(StartJobError.AllJobStepsFailedStarting);
    [Fact] void should_start_the_next_stage() => ShouldHaveStarted(_secondStepA);
    [Fact] void should_be_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
