// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_compensating_write_succeeds_before_its_acknowledgement_fails : given.a_stopped_job_rejecting_resume
{
    void Establish() => _job.BeforeResuming = () =>
    {
        _job.FailWrites = true;
        _job.PersistBeforeFailingWrite = true;
        return Task.FromException(new InvalidOperationException("Resume was rejected"));
    };

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_report_failure() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_retire_the_uncertain_activation() => _silo.GrainRuntime.Mock.Verify(runtime => runtime.DeactivateOnIdle(It.IsAny<IGrainContext>()), Times.Once);
    [Fact] void should_not_launch_work() => _step.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_leave_a_recoverable_stopped_record() => _job.WrittenStates[^1].Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_persist_stopped_progress() => _job.WrittenStates[^1].Progress.StoppedSteps.ShouldEqual(1);
}
