// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Monads;
using Cratis.Orleans.Jobs.Observation;
using Microsoft.Extensions.Logging;
using Orleans.TestKit;
using Orleans.Utilities;

namespace Cratis.Orleans.Jobs.for_Job.given;

/// <summary>
/// A job removed after it was resumed, whose first step still has a run of the resumed execution the job can tell to stop.
/// </summary>
public class a_job_being_removed_after_a_resume : a_job_being_removed_with_two_steps
{
    protected const long EarlierGeneration = 1;
    protected const long CurrentGeneration = 5;

    protected IJobObserver _currentRun;
    protected List<long?> _generationsTheCurrentRunWasToldToRemoveIn = [];

    protected int Clears => _silo.StorageStats<SomeJob, SomeJobState>().Clears;

    void Establish()
    {
        // Stopped, then running again: the resume began a new execution at position 5, before the removal.
        _job.CurrentState.StatusChanges.Insert(_job.CurrentState.StatusChanges.Count - 1, new() { Status = JobStatus.Running });
        _currentRun = Substitute.For<IJobObserver>();
        _currentRun.OnJobRemoved().Returns(_ =>
        {
            _generationsTheCurrentRunWasToldToRemoveIn.Add(RequestContext.Get(JobExecutionGeneration.Key) as long?);
            return Task.CompletedTask;
        });
        var observers = (ObserverManager<JobStepId, IJobObserver>)typeof(Job<SomeRequest, SomeJobState>)
            .GetField("_observers", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_job)!;
        observers.Subscribe(_stepId, _currentRun);
    }

    protected async Task ReactivateFrom(SomeJobState state, ILogger<Job<SomeRequest, SomeJobState>>? logger = null)
    {
        _silo = new();
        _observers = Substitute.For<IJobStepObservers>();
        _notifiableSteps.Clear();
        ConfigureSilo();
        if (logger is not null)
        {
            _silo.AddService(logger);
        }
        foreach (var step in _storedJobStepStates)
        {
            AddJobStep(step.Id.JobStepId);
        }
        _silo.StorageManager.GetStorage<SomeJobState>(typeof(SomeJob).FullName!).State = CopyState(state);
        _job = await _silo.CreateGrainAsync<SomeJob>(_jobId, _jobKey);
    }

    static SomeJobState CopyState(SomeJobState state) => new()
    {
        Id = state.Id,
        Details = state.Details,
        Type = state.Type,
        Created = state.Created,
        Status = state.Status,
        Request = state.Request,
        StatusChanges = state.StatusChanges.ToList(),
        DrainPending = state.DrainPending,
        StopFinalized = state.StopFinalized,
        StoppedStepsCounted = state.StoppedStepsCounted.ToList(),
        CompletedStepsCounted = state.CompletedStepsCounted.ToList(),
        StepPlan = state.StepPlan?.ToList(),
        Progress = state.Progress with { Stages = state.Progress.Stages.Select(stage => stage with { }).ToList() }
    };

    protected Task<Result<JobError>> ReportStopFromEarlierExecution() => ReportedIn(EarlierGeneration, () => _job.OnStepStopped(_stepId, Stopped));

    protected Task<Result<JobError>> ReportStopFromCurrentRun() => ReportedIn(CurrentGeneration, () => _job.OnStepStopped(_stepId, Stopped));

    protected Task<Result<JobError>> ReportSuccessFromCurrentRun() => ReportedIn(CurrentGeneration, () => _job.OnStepSucceeded(_stepId, JobStepResult.Succeeded()));

    protected Task<Result<JobError>> ReportFailureFromCurrentRun() => ReportedIn(CurrentGeneration, () => _job.OnStepFailed(_stepId, JobStepResult.Failed("failed")));

    static async Task<Result<JobError>> ReportedIn(long generation, Func<Task<Result<JobError>>> report)
    {
        RequestContext.Set(JobExecutionGeneration.ReportedKey, generation);
        try
        {
            return await report();
        }
        finally
        {
            RequestContext.Remove(JobExecutionGeneration.ReportedKey);
        }
    }
}
