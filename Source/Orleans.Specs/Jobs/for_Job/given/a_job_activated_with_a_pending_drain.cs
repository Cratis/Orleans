// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_Job.given;

/// <summary>
/// A job whose activation went away after it persisted the stop of a drain, activated again: one step still
/// running in storage and one already stopped.
/// </summary>
public class a_job_activated_with_a_pending_drain : the_job
{
    protected JobStepId _runningId;
    protected JobStepId _stoppedId;
    protected Mock<ISomeJobStep> _running;
    protected Mock<ISomeJobStep> _stopped;

    async Task Establish()
    {
        _runningId = JobStepId.New();
        _stoppedId = JobStepId.New();
        _running = AddJobStep(_runningId);
        _stopped = AddJobStep(_stoppedId);
        StoredJobStep(_runningId).Status = JobStepStatus.Running;
        StoredJobStep(_stoppedId).Status = JobStepStatus.Stopped;
        DrainsAs(_running, _runningId, JobStepStatus.Stopped, Stopped);
        DrainsAs(_stopped, _stoppedId, JobStepStatus.Stopped, null);
        foreach (var status in new[] { JobStatus.PreparingJob, JobStatus.PreparingSteps, JobStatus.StartingSteps, JobStatus.Running, JobStatus.Stopped })
        {
            _job.CurrentState.StatusChanges.Add(new() { Status = status });
        }
        _job.CurrentState.Status = JobStatus.Stopped;
        _job.CurrentState.DrainPending = true;
        _job.CurrentState.Progress.TotalSteps = 2;
        await _job.OnActivateAsync(CancellationToken.None);
    }
}
