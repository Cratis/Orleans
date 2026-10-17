// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_dispatch_throws_after_another_step_started : given.a_stopped_job_rejecting_resume
{
    protected override JobsOptions CreateOptions() => new() { MaxConcurrentStepStarts = 1 };

    void Establish()
    {
        _job.BeforeResuming = () => Task.CompletedTask;
        var id = JobStepId.New();
        AddJobStep(id).Setup(step => step.Start(It.IsAny<GrainId>())).ThrowsAsync(new InvalidOperationException("Start failed"));
        StoredJobStep(id).Status = JobStepStatus.Stopped;
        _job.CurrentState.Progress.TotalSteps = 2;
        _job.CurrentState.Progress.StoppedSteps = 2;
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_report_dispatch_failure() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_leave_partial_execution_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_have_started_the_first_step() => _step.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
    [Fact] void should_not_compensate_partial_dispatch() => Writes.ShouldEqual(_writesBefore + 1);
    [Fact] void should_not_restore_stopped_progress() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(0);
}
