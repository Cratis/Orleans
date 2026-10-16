// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    readonly HashSet<JobStepId> _stepsToRestartAfterEarlierStop = [];
    readonly Dictionary<JobStepId, bool> _outcomesCounted = [];
    IGrainTimer? _restartStepsTimer;

    /// <summary>
    /// Decides whether a reported step outcome is ignored, reading the generation the report carries.
    /// </summary>
    /// <param name="stepId">The <see cref="JobStepId"/> of the reporting step.</param>
    /// <param name="stopped">Whether the step reports that it stopped.</param>
    /// <param name="failed">Whether the step reports that it failed.</param>
    /// <returns>True if the outcome is ignored, false if it counts.</returns>
    async Task<bool> IgnoreStepOutcome(JobStepId stepId, bool stopped, bool failed = false)
    {
        // Read and removed first, so the generation never flows into the hooks and stages the outcome leads to. Only a
        // generation the step reported counts: one it merely inherited from its job's call says nothing about its run.
        var generation = JobExecutionGeneration.TakeReported();

        // A drain took this step's outcome from the step itself; the step's own report of it counts nothing.
        if (_accountedByDrain.Contains(stepId))
        {
            _logger.IgnoringOutcomeAccountedForByDrain(stepId);
            return true;
        }

        if (generation is { } reported && reported < ExecutionGeneration())
        {
            // A stop of an earlier execution is not a stop of this one: the step stays tracked and is started again.
            if (stopped)
            {
                _logger.IgnoringStopOfEarlierExecution(stepId, reported);
                RestartAfterEarlierStop(stepId);
                return true;
            }

            // Success and failure are facts whatever the execution, but each step counts once: only while tracked.
            if (_jobStepGrains?.ContainsKey(stepId) != true)
            {
                _logger.IgnoringOutcomeOfEarlierExecution(stepId, reported);
                await RecountAfterUntrackedOutcome(stepId, failed);
                return true;
            }
        }

        return await IgnoreLateStepOutcome(stepId);
    }

    /// <summary>
    /// Remember that this activation counted a step's success or failure.
    /// </summary>
    /// <param name="stepId">The <see cref="JobStepId"/> of the step.</param>
    /// <param name="succeeded">Whether the step succeeded.</param>
    void OutcomeCounted(JobStepId stepId, bool succeeded) => _outcomesCounted[stepId] = succeeded;

    /// <summary>
    /// Recount the job from step storage after a success or failure of an earlier execution arrived for a step the
    /// running job no longer tracks.
    /// </summary>
    /// <param name="stepId">The <see cref="JobStepId"/> of the reporting step.</param>
    /// <param name="failed">Whether the step reported that it failed.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// A resume reloads what it tracks from step storage, where a persisted success is already gone, and an activation
    /// that began after the outcome was persisted never tracked the step. Unless this activation counted the outcome
    /// itself, it cannot tell whether it is in the progress, so dropping it could leave a stage or the job one step
    /// short forever. Storage has the outcome either way, so the progress is recounted from it - in this turn, as any
    /// other outcome advances the job in the turn that reports it. A failure is reported before the step persists it,
    /// so a failure this activation counted, the reported one included, still counts while its record lags behind.
    /// </remarks>
    async Task RecountAfterUntrackedOutcome(JobStepId stepId, bool failed)
    {
        if (_outcomesCounted.ContainsKey(stepId) || State.Status is not (JobStatus.Running or JobStatus.StartingSteps))
        {
            return;
        }

        OutcomeCounted(stepId, succeeded: !failed);
        if (await ReconcileProgressFromJobSteps() is not { } steps)
        {
            return;
        }

        var counted = steps
            .Where(step =>
                step.Status is not (JobStepStatus.CompletedSuccessfully or JobStepStatus.CompletedWithFailure or JobStepStatus.Failed or JobStepStatus.Unreachable) &&
                _outcomesCounted.ContainsKey(step.Id.JobStepId))
            .Select(step => (step.Id.JobStepId, Succeeded: _outcomesCounted[step.Id.JobStepId]));
        foreach (var (id, succeeded) in counted)
        {
            if (succeeded)
            {
                State.Progress.SuccessfulSteps++;
            }
            else
            {
                State.Progress.FailedSteps++;
            }
            CountStepOutcomeInStage(id, succeeded);
        }

        // A stage the recount completed starts the next one, or stops the job at a failed barrier.
        if (await ResumeStagesLeftBetween() is null)
        {
            _ = await HandleCompletionResult(await HandleCompletion());
        }
    }

    async Task<bool> IgnoreLateStepOutcome(JobStepId stepId)
    {
        if (AcceptsStepOutcome(stepId))
        {
            return false;
        }

        _logger.IgnoringLateStepOutcome(stepId, State.Status);
        await UnsubscribeJobStep(stepId);
        _jobStepGrains?.Remove(stepId);
        return true;
    }

    bool AcceptsStepOutcome(JobStepId stepId)
    {
        // Missing and cleared records have no preparation history. Never create a job from a late report.
        if (!JobIsPrepared())
        {
            return false;
        }

        return State.Status switch
        {
            JobStatus.StartingSteps or JobStatus.Running or JobStatus.Removing => true,

            // Stop is durable before its steps report, so Stopped alone cannot reject pending outcomes.
            // Once fully stopped, or once this activation accepted this step's report, it must not count again.
            JobStatus.Stopped => !State.Progress.IsStopped && (!_hasLiveStepTracking || _jobStepGrains?.ContainsKey(stepId) == true),
            _ => false
        };
    }

    /// <summary>
    /// Start a tracked step again after its stop of an earlier execution was ignored.
    /// </summary>
    /// <param name="stepId">The <see cref="JobStepId"/> of the step.</param>
    /// <remarks>
    /// The step defers a start that arrives while a stop of an earlier execution is still settling, and runs it once
    /// the stop has settled. This covers what that cannot: an activation that went away in between, or a start that
    /// reached the step before the stop did. Starting is idempotent, so a step that is already running again only
    /// answers that it is.
    /// </remarks>
    void RestartAfterEarlierStop(JobStepId stepId)
    {
        if (State.Status is not (JobStatus.Running or JobStatus.StartingSteps) || _jobStepGrains?.ContainsKey(stepId) != true)
        {
            return;
        }

        _stepsToRestartAfterEarlierStop.Add(stepId);

        // A grain timer, so the start happens in a turn of its own once the step has had its stop report answered.
        _restartStepsTimer ??= this.RegisterGrainTimer(
            RestartStepsAfterEarlierStops,
            new GrainTimerCreationOptions { DueTime = TimeSpan.Zero, Period = Timeout.InfiniteTimeSpan });
    }

    async Task RestartStepsAfterEarlierStops(CancellationToken cancellationToken)
    {
        _restartStepsTimer?.Dispose();
        _restartStepsTimer = null;

        // A step that reported in the current execution has been untracked; it needs no start.
        var steps = _stepsToRestartAfterEarlierStop
            .Where(stepId => _jobStepGrains?.ContainsKey(stepId) == true)
            .ToDictionary(stepId => stepId, stepId => _jobStepGrains![stepId]);
        _stepsToRestartAfterEarlierStop.Clear();
        if (steps.Count == 0 || State.Status is not (JobStatus.Running or JobStatus.StartingSteps))
        {
            return;
        }

        using var scope = _logger.BeginJobScope(JobId, JobKey);
        _logger.RestartingStepsAfterEarlierStops(steps.Count);
        if ((await StartAndSubscribeToJobSteps(this.GetGrainId(), steps)).IsSuccess)
        {
            return;
        }

        // Steps that could not start were recorded and counted as failed; that may complete their stage or the job.
        foreach (var stepId in steps.Keys)
        {
            await AdvanceStagesAfter(stepId);
        }
        _ = await HandleCompletionResult(await HandleCompletion());
    }
}
