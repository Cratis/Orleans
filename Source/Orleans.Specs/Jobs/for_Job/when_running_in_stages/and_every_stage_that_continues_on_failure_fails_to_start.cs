// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_running_in_stages;

public class and_every_stage_that_continues_on_failure_fails_to_start : given.a_job_with_three_stages
{
    Result<StartJobError> _result;

    void Establish()
    {
        _job.StagesContinuingOnFailure.Add(0);
        _job.StagesContinuingOnFailure.Add(10);
        _firstStep.Setup(_ => _.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Failed(StartJobStepError.NotPrepared));
        _secondStepA.Setup(_ => _.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Failed(StartJobStepError.NotPrepared));
        _secondStepB.Setup(_ => _.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Failed(StartJobStepError.NotPrepared));
        _thirdStep.Setup(_ => _.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Failed(StartJobStepError.NotPrepared));
    }

    async Task Because() => _result = await _job.Start(new());

    [Fact] void should_report_that_no_step_started() => ((StartJobError)_result).ShouldEqual(StartJobError.AllJobStepsFailedStarting);
    [Fact] void should_complete_the_job_with_failures() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedWithFailures);
    [Fact] void should_count_every_step_as_failed() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(4);
}
