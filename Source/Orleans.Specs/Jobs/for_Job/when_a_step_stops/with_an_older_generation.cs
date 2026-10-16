// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_a_step_stops;

public class with_an_older_generation : given.the_job
{
    const string GenerationKey = "Cratis.Orleans.Jobs.Generation";

    JobStepId _stepId;
    Mock<given.ISomeJobStep> _step;
    Result<JobError> _result;

    async Task Establish()
    {
        _stepId = JobStepId.New();
        _step = AddJobStep(_stepId);
        _step.Setup(step => step.Start(It.IsAny<GrainId>())).ReturnsAsync(StartJobStepError.AlreadyStarted);
        await _job.Start(new());

        // The job was stopped and resumed, which began a later generation than the one its step was started in.
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Stopped });
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Running });
    }

    async Task Because()
    {
        RequestContext.Set(GenerationKey, 1L);
        try
        {
            _result = await _job.OnStepStopped(_stepId, JobStepResult.Failed(PerformJobStepError.CancelledWithNoResult()));
        }
        finally
        {
            RequestContext.Remove(GenerationKey);
        }
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_acknowledge_the_report() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_count_the_stop() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(0);
    [Fact] void should_not_hand_the_stop_to_the_job() => _job.HandledStepResults.ShouldBeEmpty();
    [Fact] void should_keep_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_start_the_step_again() => _step.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Exactly(2));
}
