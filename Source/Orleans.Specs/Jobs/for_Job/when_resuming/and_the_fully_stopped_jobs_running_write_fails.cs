// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_the_fully_stopped_jobs_running_write_fails : given.a_stopped_job_rejecting_resume
{
    void Establish()
    {
        _job.FailWrites = true;
        _job.PersistBeforeFailingWrite = true;
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_report_failure() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_run_the_hook_under_an_uncertain_write() => _job.BeforeResumingCount.ShouldEqual(0);
    [Fact] void should_retire_the_uncertain_activation() => _silo.GrainRuntime.Mock.Verify(runtime => runtime.DeactivateOnIdle(It.IsAny<IGrainContext>()), Times.Once);
    [Fact] void should_not_dispatch_work() => _step.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_restore_accounted_progress_before_retiring() => _job.CurrentState.Progress.StoppedSteps.ShouldEqual(1);
}
