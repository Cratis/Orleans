// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_completing_with_failures;

public class and_the_job_does_not_keep_failed_runs : given.the_job
{
    JobStepId _jobStepId;

    void Establish()
    {
        _job.ShouldBeRemovedAfterCompletedWithFailures = true;
        _jobStepId = Guid.Parse("6c6c6c6c-0000-0000-0000-000000000001");
        var step = AddJobStep(_jobStepId);
        step.Setup(_ => _.Start(It.IsAny<GrainId>())).ReturnsAsync(Cratis.Monads.Result<StartJobStepError>.Failed(StartJobStepError.AlreadyStarted));
    }

    async Task Because()
    {
        await _job.Start(new());
        await _job.OnStepFailed(_jobStepId, JobStepResult.Failed("failed"));
    }

    [Fact] void should_clear_the_state() => _job.CurrentState.Progress.TotalSteps.ShouldEqual(0);
}
