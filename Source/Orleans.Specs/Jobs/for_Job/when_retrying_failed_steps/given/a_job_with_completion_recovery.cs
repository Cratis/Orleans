// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_retrying_failed_steps.given;

public class a_job_with_completion_recovery : for_Job.given.a_job_ready_to_complete
{
    protected Mock<for_Job.given.ISomeJobStep> _failed;
    protected Result<int, RetryFailedStepsError> _result;

    void Establish()
    {
        var failedId = JobStepId.New();
        _failed = AddJobStep(failedId);
        _failed.Setup(step => step.Retry(It.IsAny<GrainId>(), It.IsAny<int>()))
            .ReturnsAsync(Result<StartJobStepError>.Failed(StartJobStepError.AlreadyStarted));
        StoredJobStep(failedId).Status = JobStepStatus.CompletedWithFailure;
        StoredJobStep(failedId).Attempt = 1;
        _job.RetryFailedStepsAllowed = true;
        _job.CurrentState.Progress.SuccessfulSteps = 0;
        _job.CurrentState.Progress.FailedSteps = 1;
    }
}
