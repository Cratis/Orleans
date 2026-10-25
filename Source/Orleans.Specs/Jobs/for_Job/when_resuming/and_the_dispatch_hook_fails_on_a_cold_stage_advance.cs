// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_dispatch_hook_fails_on_a_cold_stage_advance : given.a_job_with_a_durable_plan
{
    bool _startedAfterFailure;

    protected override JobsOptions CreateOptions() => new() { MaxConcurrentStepStarts = 1, JobWatchdogInterval = TimeSpan.Zero };

    async Task Establish()
    {
        await _job.Start(new());
        await ActivateFrom(JobStatus.Running);
        _job.BeforeDispatching = _ => Task.FromException(new InvalidOperationException("Initialization failed"));
        (await _job.OnStepSucceeded(_firstId, JobStepResult.Succeeded())).IsSuccess.ShouldBeTrue();
    }

    async Task Because()
    {
        (await _job.OnStepSucceeded(_secondId, JobStepResult.Succeeded())).IsSuccess.ShouldBeFalse();
        _startedAfterFailure = _job.CurrentState.Progress.Stages[1].IsStarted;
        _later.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
        _job.BeforeDispatching = null;
        _result = await _job.Resume();
    }

    [Fact] void should_leave_the_next_stage_unstarted_when_initialization_fails() => _startedAfterFailure.ShouldBeFalse();
    [Fact] void should_resume_the_stage_after_initialization_succeeds() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_dispatch_the_next_stage_once() => _later.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
}
