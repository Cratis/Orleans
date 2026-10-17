// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_retrying_failed_steps.given;

public class a_job_that_completed_with_failures : for_Job.given.the_job
{
    protected JobStepId _failedId;
    protected JobStepId _succeededId;
    protected Mock<for_Job.given.ISomeJobStep> _failed;

    void Establish()
    {
        _failedId = JobStepId.New();
        _succeededId = JobStepId.New();
        _failed = AddJobStep(_failedId);
        _failed.Setup(_ => _.Retry(It.IsAny<GrainId>(), It.IsAny<int>())).ReturnsAsync(Cratis.Monads.Result<StartJobStepError>.Failed(StartJobStepError.AlreadyStarted));
        StoredJobStep(_failedId).Status = JobStepStatus.CompletedWithFailure;
        StoredJobStep(_failedId).Attempt = 1;
        foreach (var status in new[] { JobStatus.PreparingJob, JobStatus.PreparingSteps, JobStatus.StartingSteps, JobStatus.Running, JobStatus.CompletedWithFailures })
        {
            _job.CurrentState.StatusChanges.Add(new() { Status = status });
        }
        _job.CurrentState.Status = JobStatus.CompletedWithFailures;
        _job.CurrentState.Progress.TotalSteps = 2;
        _job.CurrentState.Progress.SuccessfulSteps = 1;
        _job.CurrentState.Progress.FailedSteps = 1;
    }
}
