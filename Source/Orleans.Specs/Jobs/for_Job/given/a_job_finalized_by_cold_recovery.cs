// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Monads;
using Orleans.TestKit;
using Orleans.Utilities;

namespace Cratis.Orleans.Jobs.for_Job.given;

public class a_job_finalized_by_cold_recovery : the_job
{
    protected JobStepId _stepId;
    protected Result<ResumeJobSuccess, ResumeJobError> _resume;
    protected Result<JobError> _report;
    protected JobProgress _progressBeforeReport;
    protected JobStatus _statusBeforeReport;
    protected ObserverManager<JobStepId, IJobObserver> _observers;

    protected virtual bool KeepRecoveredJob => false;

    async Task Establish()
    {
        _stepId = JobStepId.New();
        _job.ShouldBeRemovedAfterCompleted = !KeepRecoveredJob;
        _job.CurrentState.Status = JobStatus.Running;
        _job.CurrentState.StatusChanges.Add(new() { Status = JobStatus.StartingSteps });
        _job.CurrentState.Progress.TotalSteps = 1;

        // A surviving step deleted its successful state, but its report is behind Resume in the job's queue.
        // This activation has never started or accepted an outcome from a step.
        _resume = await _job.Resume();
        _progressBeforeReport = _job.CurrentState.Progress with { };
        _statusBeforeReport = _job.CurrentState.Status;
        _silo.StorageStats<SomeJob, SomeJobState>().ResetCounts();
        _observers = (ObserverManager<JobStepId, IJobObserver>)typeof(Job<SomeRequest, SomeJobState>)
            .GetField("_observers", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_job)!;
        _observers.Subscribe(_stepId, Substitute.For<IJobObserver>());
    }
}
