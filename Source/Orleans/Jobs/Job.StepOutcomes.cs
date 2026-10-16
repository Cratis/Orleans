// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    readonly HashSet<JobStepId> _stepsToRestartAfterEarlierStop = [];
    IGrainTimer? _restartStepsTimer;

    /// <summary>
    /// Decides whether a reported step outcome is ignored, reading the generation the report carries.
    /// </summary>
    /// <param name="stepId">The <see cref="JobStepId"/> of the reporting step.</param>
    /// <param name="stopped">Whether the step reports that it stopped.</param>
    /// <returns>True if the outcome is ignored, false if it counts.</returns>
    async Task<bool> IgnoreStepOutcome(JobStepId stepId, bool stopped)
    {
        // Read and removed first, so the generation never flows into the hooks and stages the outcome leads to.
        var generation = JobExecutionGeneration.Take();
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
                return true;
            }
        }

        return await IgnoreLateStepOutcome(stepId);
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
