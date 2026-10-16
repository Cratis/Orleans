// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.given;

public class a_job_left_running : for_Job.given.the_job
{
    protected JobStepId _unfinishedId;
    protected JobStepId _completedId;
    protected JobStepId _stoppedId;
    protected Mock<for_Job.given.ISomeJobStep> _unfinished;
    protected Mock<for_Job.given.ISomeJobStep> _completed;
    protected Mock<for_Job.given.ISomeJobStep> _stopped;
    protected Result<ResumeJobSuccess, ResumeJobError> _result;

    void Establish()
    {
        _unfinishedId = JobStepId.New();
        _completedId = JobStepId.New();
        _stoppedId = JobStepId.New();
        _unfinished = AddJobStep(_unfinishedId);
        _completed = AddJobStep(_completedId);
        _stopped = AddJobStep(_stoppedId);
        _unfinished.Setup(step => step.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Success());
        StoredJobStep(_unfinishedId).Status = JobStepStatus.Running;
        StoredJobStep(_completedId).Status = JobStepStatus.CompletedSuccessfully;
        StoredJobStep(_stoppedId).Status = JobStepStatus.Stopped;
        _job.CurrentState.Status = JobStatus.Running;
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.StartingSteps });
        _job.CurrentState.Progress.TotalSteps = 3;
        _job.CurrentState.Progress.SuccessfulSteps = 1;
        _job.CurrentState.Progress.StoppedSteps = 1;
    }
}
