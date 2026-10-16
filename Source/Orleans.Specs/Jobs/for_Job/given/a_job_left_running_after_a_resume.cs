// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.given;

public class a_job_left_running_after_a_resume : a_job_left_running
{
    void Establish()
    {
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Running });
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Stopped });
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Running });
        _job.CurrentState.Progress.StoppedSteps = 0;
        StoredJobStep(_unfinishedId).Status = JobStepStatus.Stopped;
        _stopped.Setup(step => step.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Success());
    }
}
