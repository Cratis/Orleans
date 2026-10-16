// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_resuming;

public class and_an_already_reported_failure_is_still_in_storage : given.the_job
{
    JobStepId _failedId;
    Mock<given.ISomeJobStep> _failed;
    Mock<given.ISomeJobStep> _live;

    async Task Establish()
    {
        _failedId = JobStepId.New();
        _failed = AddJobStep(_failedId);
        _live = AddJobStep(JobStepId.New());
        foreach (var step in new[] { _failed, _live })
        {
            step.Setup(grain => grain.Start(It.IsAny<GrainId>())).ReturnsAsync(StartJobStepError.AlreadyStarted);
        }
        await _job.Start(new());
        StoredJobStep(_failedId).Status = JobStepStatus.Running;
        await _job.OnStepFailed(_failedId, JobStepResult.Failed(PerformJobStepError.Failed(["failure"], string.Empty)));
        _failed.Invocations.Clear();
        _live.Invocations.Clear();
    }

    async Task Because() => await _job.Resume();

    [Fact] void should_not_restart_the_reported_step() => _failed.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_check_the_live_step() => _live.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
    [Fact] void should_keep_the_failure_count() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(1);
}
