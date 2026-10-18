// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Orleans.Jobs.Stages;

namespace Cratis.Orleans.Jobs.for_Job.given;

public class SomeJob : Job<SomeRequest, SomeJobState>, IGrainType
{
    public List<JobStepDetails> StepsToPrepare = [];
    public bool OnCompletedThrows;
    public bool ShouldBeRemovedAfterCompleted;
    public bool ShouldBeRemovedAfterCompletedWithFailures;
    public bool ShouldBeResumable;
    public Func<Task>? BeforeStarting;
    public int BeforeStartingCount;
    public Func<JobStepDispatchContext, Task>? BeforeDispatching;
    public List<JobStepDispatchContext> DispatchContexts = [];
    public Func<SomeJobState, bool>? RejectWrite;
    public Func<Task<IImmutableList<JobStepDetails>>>? PlanSteps;
    public Func<Task>? AfterWrite;
    public Func<Task>? BeforeResuming;
    public int BeforeResumingCount;
    public HashSet<JobStepStage> StagesContinuingOnFailure = [];

    public int? Window;
    public Func<JobStepId, JobStepResult, int, bool>? RetryWhen;
    public bool RetryFailedStepsAllowed;
    public ISet<string>? ActiveSiloAddresses;

    public Type GrainType => typeof(ISomeJob);

    public SomeJobState CurrentState => State;

    public int PrepareStepsCount;

    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(SomeRequest request)
    {
        PrepareStepsCount++;
        return PlanSteps?.Invoke() ?? Task.FromResult<IImmutableList<JobStepDetails>>(StepsToPrepare.ToImmutableList());
    }

    protected override bool KeepAfterCompleted => !ShouldBeRemovedAfterCompleted;
    protected override bool KeepAfterCompletedWithFailures => !ShouldBeRemovedAfterCompletedWithFailures;
    public int AllStepsCompletedCount;
    public int StoppedCount;
    public bool FailWrites;
    public int WriteAttempts;
    public bool PersistBeforeFailingWrite;
    public List<SomeJobState> WrittenStates = [];
    public List<JobStepId> HandledStepResults = [];
    public int ResultsSeenAtCompletion;
    public int SuccessfulStepsAtCompletion;
    public int TotalStepsAtCompletion;

    protected override Task OnStepCompletedOrStopped(JobStepId jobStepId, JobStepResult result)
    {
        HandledStepResults.Add(jobStepId);
        return Task.CompletedTask;
    }

    protected override Task OnAllStepsCompleted()
    {
        AllStepsCompletedCount++;
        ResultsSeenAtCompletion = HandledStepResults.Count;
        SuccessfulStepsAtCompletion = State.Progress.SuccessfulSteps;
        TotalStepsAtCompletion = State.Progress.TotalSteps;
        return OnCompletedThrows
            ? Task.FromException(new Exception())
            : Task.CompletedTask;
    }

    public TimeSpan DelayAfter(int failedAttempt) => GetStepRetryDelay(failedAttempt);

    protected override int? MaxActiveSteps => Window ?? base.MaxActiveSteps;

    protected override bool ShouldRetryStep(JobStepId stepId, JobStepResult result, int attempt) =>
        RetryWhen?.Invoke(stepId, result, attempt) ?? base.ShouldRetryStep(stepId, result, attempt);

    protected override bool CanRetryFailedSteps => RetryFailedStepsAllowed;

    protected override Task<ISet<string>?> GetActiveSilos() => Task.FromResult(ActiveSiloAddresses);

    protected override Task<bool> CanResume() => Task.FromResult(ShouldBeResumable);

    protected override Task OnBeforeStartingJobSteps()
    {
        BeforeStartingCount++;
        return BeforeStarting?.Invoke() ?? Task.CompletedTask;
    }

    protected override async Task OnBeforeDispatchingJobSteps(JobStepDispatchContext context)
    {
        if (BeforeDispatching is not null)
        {
            await BeforeDispatching(context);
        }
        DispatchContexts.Add(context);
    }

    protected override Task OnBeforeResumingJobSteps()
    {
        BeforeResumingCount++;
        return BeforeResuming?.Invoke() ?? Task.CompletedTask;
    }

    protected override Task OnStopped()
    {
        StoppedCount++;
        return Task.CompletedTask;
    }

    protected override async Task WriteStateAsync()
    {
        WriteAttempts++;
        var rejected = FailWrites || RejectWrite?.Invoke(State) == true;
        if (!rejected || PersistBeforeFailingWrite)
        {
            await base.WriteStateAsync();
            WrittenStates.Add(new()
            {
                Id = State.Id,
                Type = State.Type,
                Request = State.Request,
                Status = State.Status,
                StatusChanges = State.StatusChanges.ToList(),
                DrainPending = State.DrainPending,
                StepPlan = State.StepPlan?.ToList(),
                Progress = State.Progress with { Stages = State.Progress.Stages.Select(stage => stage with { }).ToList() }
            });
        }
        if (rejected)
        {
            throw new InvalidOperationException("The write failed");
        }
        if (AfterWrite is not null)
        {
            await AfterWrite();
        }
    }

    protected override JobStageFailureBehavior GetFailureBehaviorFor(JobStepStage stage) =>
        StagesContinuingOnFailure.Contains(stage) ? JobStageFailureBehavior.ContinueWithNextStage : JobStageFailureBehavior.StopJob;
}