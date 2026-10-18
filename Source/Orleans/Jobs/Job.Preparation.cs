// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Monads;
using Cratis.Orleans.Jobs.Stages;
using Cratis.Orleans.Serialization;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Orleans.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    bool _dispatchReady;
    bool _hasDispatchedSteps;

    JsonSerializerOptions StepPlanSerializerOptions =>
        ServiceProvider.GetService<JsonSerializerOptions>() ?? CratisJsonSerializer.CreateSerializerOptions();

    List<PlannedJobStep> SaveStepPlan(IImmutableList<JobStepDetails> steps) =>
        steps.Select(step => new PlannedJobStep(
            step.Id,
            step.Type,
            step.Stage,
            step.Request.GetType().AssemblyQualifiedName!,
            JsonSerializer.Serialize(step.Request, step.Request.GetType(), StepPlanSerializerOptions))).ToList();

    async Task RunDispatchHook(bool initializationMayBeIncomplete)
    {
        _dispatchReady = false;
        await OnBeforeDispatchingJobSteps(new(initializationMayBeIncomplete));
        _dispatchReady = true;
    }

    Task EnsureDispatchHook() => _dispatchReady
        ? Task.CompletedTask
        : RunDispatchHook(State.Status is JobStatus.PreparingSteps or JobStatus.StartingSteps);

    async Task<Result<ResumeJobSuccess, ResumeJobError>> RecoverStepPreparation()
    {
        if (State.StepPlan is not { } plan)
        {
            return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.MissingStepPlan);
        }

        var stored = await Storage.JobSteps.GetForJob(JobId);
        stored.RethrowError();
        var prepared = stored.AsT0.Where(step => step.IsPrepared).Select(step => step.Id.JobStepId).ToHashSet();
        var pending = plan.Where(step => !prepared.Contains(step.Id)).ToImmutableList();
        var steps = pending.Select(step => new JobStepDetails(
            step.Type,
            step.Id,
            new(JobId, JobKey.Scope, JobKey.Namespace),
            JsonSerializer.Deserialize(step.RequestJson, Type.GetType(step.RequestType, throwOnError: true)!, StepPlanSerializerOptions)!,
            typeof(object)) { Stage = step.Stage }).ToImmutableList();

        // No step can have executed in PreparingSteps. Missing state here is an unprepared step, not a success.
        State.Progress.TotalSteps = plan.Count;
        State.Progress.Stages = JobStages.Plan(plan.Select(step => step.Stage));
        foreach (var step in plan)
        {
            _jobStepStages[step.Id] = step.Stage;
        }
        if (!await TryWriteStatusChanged(JobStatus.PreparingSteps))
        {
            return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.Unknown);
        }
        var preparation = await TryPrepareAllJobSteps(CreateGrainsFromJobSteps(steps));
        if (!preparation.TryGetResult(out var allPrepared) || !allPrepared)
        {
            return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.JobCannotBeResumed);
        }
        if (!await TryWriteStatusChanged(JobStatus.StartingSteps))
        {
            return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.Unknown);
        }
        return await RecoverStepStartup();
    }

    async Task<Result<ResumeJobSuccess, ResumeJobError>> RecoverStepStartup()
    {
        if (State.StepPlan is not { } plan)
        {
            return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.MissingStepPlan);
        }

        // StartingSteps is durable only after every step was prepared. Successful steps may already have deleted
        // their state during a partial dispatch; never recreate them from the plan.
        var stored = await Storage.JobSteps.GetForJob(JobId);
        stored.RethrowError();
        var steps = stored.AsT0;
        var byId = steps.ToDictionary(step => step.Id.JobStepId);
        State.Progress.TotalSteps = plan.Count;
        State.Progress.SuccessfulSteps = plan.Count(step => !byId.TryGetValue(step.Id, out var state) || state.Status is JobStepStatus.CompletedSuccessfully);
        State.Progress.FailedSteps = steps.Count(step => step.Status is JobStepStatus.Failed or JobStepStatus.CompletedWithFailure);
        State.Progress.UnreachableSteps = steps.Count(step => step.Status is JobStepStatus.Unreachable);
        State.Progress.StoppedSteps = 0;
        State.Progress.Stages = JobStages.Plan(plan.Select(step => step.Stage));
        foreach (var step in plan)
        {
            _jobStepStages[step.Id] = step.Stage;
            var stage = State.Progress.Stages.FirstOrDefault(progress => progress.Stage == step.Stage);
            if (stage is null)
            {
                continue;
            }
            if (!byId.TryGetValue(step.Id, out var state) || state.Status is JobStepStatus.CompletedSuccessfully)
            {
                stage.SuccessfulSteps++;
            }
            else if (state.Status is JobStepStatus.Failed or JobStepStatus.CompletedWithFailure)
            {
                stage.FailedSteps++;
            }
            else if (state.Status is JobStepStatus.Unreachable)
            {
                stage.UnreachableSteps++;
            }
        }
        _jobStepGrains = steps.Where(step => !IsDone(step.Status)).ToDictionary(step => step.Id.JobStepId, GetJobStepGrain);

        // Reconstructed terminal outcomes are already in the counters. Delayed reports must not count them again.
        _countsFromStorage = true;
        foreach (var step in plan.Where(step => !_jobStepGrains.ContainsKey(step.Id)))
        {
            OutcomeCounted(step.Id);
        }
        await OnBeforeResumingJobSteps();
        await RunDispatchHook(initializationMayBeIncomplete: true);
        if (State.Progress.IsCompleted || await PrepareStagesForResume())
        {
            return await FinalizeInsteadOfResuming();
        }

        var resumed = await ResumeCurrentStage(StepsInCurrentStage(), reportAlreadyRunning: false);
        if (State.Status is JobStatus.CompletedSuccessfully or JobStatus.CompletedWithFailures)
        {
            return ResumeJobSuccess.JobIsCompleted;
        }
        if (!await TryWriteStatusChanged(JobStatus.Running))
        {
            return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.Unknown);
        }
        return resumed;
    }
}
