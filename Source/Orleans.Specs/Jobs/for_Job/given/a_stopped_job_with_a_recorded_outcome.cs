// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.given;

public class a_stopped_job_with_a_recorded_outcome : a_stopped_job_rejecting_resume
{
    protected JobStepId _completedId;

    void Establish()
    {
        _completedId = JobStepId.New();
        _job.CurrentState.CompletedStepsCounted = [_completedId];
        _job.CurrentState.Progress.TotalSteps = 2;
        _job.CurrentState.Progress.SuccessfulSteps = 1;
    }
}
