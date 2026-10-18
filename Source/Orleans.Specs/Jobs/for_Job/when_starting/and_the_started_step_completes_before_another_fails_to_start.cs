// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_starting;

public class and_the_started_step_completes_before_another_fails_to_start : given.the_job
{
    Result<StartJobError> _result;

    void Establish()
    {
        var completingStepId = JobStepId.New();
        var completingStep = AddJobStep(completingStepId);
        var failingStep = AddJobStep(JobStepId.New());
        completingStep.Setup(_ => _.Start(It.IsAny<GrainId>())).Returns(async () =>
        {
            await _job.OnStepSucceeded(completingStepId, JobStepResult.Succeeded());
            return Result<StartJobStepError>.Failed(StartJobStepError.AlreadyStarted);
        });
        failingStep.Setup(_ => _.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Failed(StartJobStepError.NotPrepared));
    }

    async Task Because() => _result = await _job.Start(new());

    [Fact] void should_still_report_the_partial_start_failure() => ((StartJobError)_result).ShouldEqual(StartJobError.FailedStartingSomeJobSteps);
    [Fact] void should_complete_the_job_with_failures() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedWithFailures);
    [Fact] void should_persist_completion() => _job.WrittenStates[^1].Status.ShouldEqual(JobStatus.CompletedWithFailures);
    [Fact] void should_count_the_successful_step() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_count_the_failed_step() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(1);
    [Fact] void should_complete_once() => _job.AllStepsCompletedCount.ShouldEqual(1);
    [Fact] void should_not_reopen_the_job() => _job.WrittenStates.Exists(_ => _.Status == JobStatus.Running).ShouldBeFalse();
    [Fact] void should_not_arm_a_watchdog_for_the_completed_job() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
