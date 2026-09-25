// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Jobs.Stages;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    readonly Dictionary<JobStepId, JobStepStage> _jobStepStages = [];

    /// <summary>
    /// Gets whether the job's steps run in more than one stage.
    /// </summary>
    bool IsStaged => State.Progress.Stages.Count > 1;

    /// <summary>
    /// Create a new <see cref="IJobStep"/> that runs in a given stage.
    /// </summary>
    /// <param name="request">The request associated with the step.</param>
    /// <param name="stage">The <see cref="JobStepStage"/> the step runs in.</param>
    /// <typeparam name="TJobStep">The type of job step to create.</typeparam>
    /// <returns>A new instance of the job step.</returns>
    /// <remarks>
    /// Every step in a stage reaches an outcome before any step in a later stage starts; steps within a stage run
    /// in parallel. All steps are still prepared up front, so preparing a step in a later stage cannot observe the
    /// effects of an earlier stage - anything it needs from one has to be read when the step is performed.
    /// </remarks>
    protected JobStepDetails CreateStep<TJobStep>(object request, JobStepStage stage)
        where TJobStep : IJobStep => CreateStep<TJobStep>(request) with { Stage = stage };

    /// <summary>
    /// Get what the job does after a stage in which one or more steps failed.
    /// </summary>
    /// <param name="stage">The <see cref="JobStepStage"/> that had failures.</param>
    /// <returns>The <see cref="JobStageFailureBehavior"/> for the stage.</returns>
    /// <remarks>
    /// Defaults to <see cref="JobStageFailureBehavior.StopJob"/> for every stage. Override for a job that mixes
    /// best-effort stages with barriers.
    /// </remarks>
    protected virtual JobStageFailureBehavior GetFailureBehaviorFor(JobStepStage stage) => JobStageFailureBehavior.StopJob;

    void PlanStages(IImmutableList<JobStepDetails> steps)
    {
        _jobStepStages.Clear();
        foreach (var step in steps)
        {
            _jobStepStages[step.Id] = step.Stage;
        }
        State.Progress.Stages = JobStages.Plan(steps.Select(_ => _.Stage));
    }

    void TrackStagesOf(IEnumerable<JobStepState> steps)
    {
        foreach (var step in steps)
        {
            _jobStepStages[step.Id.JobStepId] = step.Stage;
        }
    }

    void CountStepOutcomeInStage(JobStepId stepId, bool succeeded)
    {
        if (StageOf(stepId) is not { } progress)
        {
            return;
        }

        if (succeeded)
        {
            progress.SuccessfulSteps++;
        }
        else
        {
            progress.FailedSteps++;
        }
    }

    JobStageProgress? StageOf(JobStepId stepId) =>
        IsStaged && _jobStepStages.TryGetValue(stepId, out var stage)
            ? State.Progress.Stages.FirstOrDefault(_ => _.Stage == stage)
            : null;

    /// <summary>
    /// Get the tracked steps that are to run now - every one of them for a job without stages, those in the stage
    /// that is currently running for a job with them.
    /// </summary>
    /// <returns>The steps to run by their <see cref="JobStepId"/>.</returns>
    Dictionary<JobStepId, IJobStep> StepsInCurrentStage()
    {
        if (!IsStaged)
        {
            return _jobStepGrains ?? [];
        }

        return State.Progress.Stages.Current() is { } current
            ? StepsIn(current.Stage)
            : [];
    }

    Dictionary<JobStepId, IJobStep> StepsIn(JobStepStage stage) =>
        (_jobStepGrains ?? [])
            .Where(_ => _jobStepStages.TryGetValue(_.Key, out var stepStage) && stepStage == stage)
            .ToDictionary(_ => _.Key, _ => _.Value);

    /// <summary>
    /// Start the steps that run first - all of them for a job without stages, those in the first stage for a job
    /// with them.
    /// </summary>
    /// <param name="grainId">The <see cref="GrainId"/> of the job.</param>
    /// <returns>The outcome of starting the steps in the first stage.</returns>
    /// <remarks>
    /// Later stages report their start failures through the job rather than to the caller of
    /// <see cref="Start"/>, so the result describes the first stage alone.
    /// </remarks>
    async Task<Result<StartJobError>> StartFirstStage(GrainId grainId)
    {
        if (!IsStaged)
        {
            return await StartAndSubscribeToJobSteps(grainId, _jobStepGrains!);
        }

        var first = State.Progress.Stages[0];
        var result = await StartStage(grainId, first);
        await AdvanceStagesAfter(first);
        return result;
    }

    Task<Result<StartJobError>> StartStage(GrainId grainId, JobStageProgress stage)
    {
        _logger.StartingStage(stage.Stage, stage.TotalSteps);
        stage.IsStarted = true;
        return StartAndSubscribeToJobSteps(grainId, StepsIn(stage.Stage));
    }

    /// <summary>
    /// Move the job on once the stage a step belonged to has completed - start the next stage, or stop the job when
    /// the completed stage is a barrier that had failures.
    /// </summary>
    /// <param name="stepId">The <see cref="JobStepId"/> of the step that reached an outcome.</param>
    /// <returns>Awaitable task.</returns>
    async Task AdvanceStagesAfter(JobStepId stepId)
    {
        if (StageOf(stepId) is { } stage)
        {
            await AdvanceStagesAfter(stage);
        }
    }

    async Task AdvanceStagesAfter(JobStageProgress stage)
    {
        var grainId = this.GetGrainId();

        // A stage whose steps all fail to start completes the moment it starts, so keep going until a stage is
        // actually running or there is nothing left to start.
        while (stage.IsCompleted && State.Status is JobStatus.Running or JobStatus.StartingSteps)
        {
            if (await StopAtFailedBarrier() || State.Progress.Stages.Current() is not { } next)
            {
                return;
            }

            _ = await StartStage(grainId, next);
            stage = next;
        }
    }

    /// <summary>
    /// Stop the job when a completed stage with failures is a barrier, recording every step behind it as unreachable.
    /// </summary>
    /// <returns>True if the job was stopped at a failed barrier, false if not.</returns>
    async Task<bool> StopAtFailedBarrier()
    {
        if (State.Progress.Stages.StoppedBy(GetFailureBehaviorFor) is not { } failedStage)
        {
            return false;
        }

        _logger.StageFailed(failedStage.Stage, failedStage.FailedSteps);
        foreach (var stage in State.Progress.Stages.Where(_ => !_.IsCompleted))
        {
            foreach (var (id, grain) in StepsIn(stage.Stage))
            {
                await RecordJobStepStatus(id, grain, JobStepStatus.Unreachable);
                _jobStepGrains?.Remove(id);
                _jobStepStages.Remove(id);
            }
            State.Progress.UnreachableSteps += stage.RemainingSteps;
            stage.UnreachableSteps += stage.RemainingSteps;
        }
        return true;
    }

    /// <summary>
    /// Pick up a running job with stages that a stopped silo left between two stages - one stage completed, and the
    /// next one never started or the job never stopped at the failed barrier.
    /// </summary>
    /// <returns>The <see cref="ResumeJobSuccess"/> if the job was picked up, or null if it was not left between stages.</returns>
    async Task<ResumeJobSuccess?> ResumeStagesLeftBetween()
    {
        if (!IsStaged || State.Progress.Stages.Current() is { IsStarted: true })
        {
            return null;
        }

        if (!await StopAtFailedBarrier() && State.Progress.Stages.Current() is { } current)
        {
            _ = await StartStage(this.GetGrainId(), current);
            await AdvanceStagesAfter(current);
        }

        _ = await HandleCompletionResult(await HandleCompletion());
        return State.Progress.IsCompleted ? ResumeJobSuccess.JobIsCompleted : ResumeJobSuccess.Success;
    }

    /// <summary>
    /// Get the job's stages ready to resume: stop at a failed barrier a crash left unacted on, and recount from the
    /// steps themselves when the stage that should be running has nothing left to run.
    /// </summary>
    /// <returns>True if the job has nothing left to resume, false if not.</returns>
    async Task<bool> PrepareStagesForResume()
    {
        if (!IsStaged)
        {
            return false;
        }

        if (await StopAtFailedBarrier())
        {
            return true;
        }

        if (State.Progress.Stages.Current() is { } current && StepsIn(current.Stage).Count == 0)
        {
            await ReconcileProgressFromJobSteps();
            if (await StopAtFailedBarrier() || State.Progress.IsCompleted)
            {
                return true;
            }
        }

        State.Progress.Stages.Current()!.IsStarted = true;
        return false;
    }
}
