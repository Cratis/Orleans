// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.given;

/// <summary>
/// A job being removed whose two steps have not reported their stops yet, as a fresh activation finds it.
/// </summary>
public class a_job_being_removed_with_two_steps : a_job_whose_silo_went_away_while_stopping
{
    protected JobStepId _secondStepId;

    void Establish()
    {
        _secondStepId = Guid.Parse("7d7d7d7d-0000-0000-0000-000000000002");
        StoredJobStep(_stepId).Status = JobStepStatus.Running;
        StoredJobStep(_secondStepId).Status = JobStepStatus.Running;
        _job.CurrentState.Status = JobStatus.Removing;
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.Removing });
        _job.CurrentState.Progress.TotalSteps = 2;
    }
}
