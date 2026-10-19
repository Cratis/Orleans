// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    readonly HashSet<JobStepId> _stepsToRestartAfterEarlierStop = [];
    readonly HashSet<JobStepId> _outcomesCounted = [];
    readonly HashSet<JobStepId> _stopsCounted = [];
    IGrainTimer? _restartStepsTimer;

    /// <summary>
    /// Decides whether a reported step outcome is ignored, reading the generation the report carries.
    /// </summary>
    /// <param name="stepId">The <see cref="JobStepId"/> of the reporting step.</param>
    /// <param name="stopped">Whether the step reports that it stopped.</param>
    /// <returns>Whether the outcome is ignored, and the attempt it was reported for.</returns>
    async Task<(bool Ignored, int Attempt)> IgnoreStepOutcome(JobStepId stepId, bool stopped)
    {
        // Read and removed first, so neither the generation nor the attempt flows into the hooks and stages the outcome
        // leads to. Only a generation the step reported counts: one it merely inherited from its job's call says nothing
        // about its run.
        var attempt = JobStepAttempt.TakeReported();
        return (await IgnoreStepOutcome(stepId, stopped, attempt), attempt);
    }

    async Task<bool> IgnoreStepOutcome(JobStepId stepId, bool stopped, int attempt)
    {
        var generation = JobExecutionGeneration.TakeReported();

        // A drain took this step's outcome from the step itself; the step's own report of it counts nothing.
        if (_accountedByDrain.Contains(stepId))
        {
            _logger.IgnoringOutcomeAccountedForByDrain(stepId);
            return true;
        }

        // A step delivers its outcome until it is acknowledged, so the same outcome can arrive again: once for each
        // attempt is enough, and what storage already accounted for after a restart is not counted a second time.
        if (!stopped && IsDuplicateOutcome(stepId, attempt))
        {
            _logger.IgnoringDuplicateOutcome(stepId, attempt);
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

            // Success and failure are facts whatever the execution, and are counted as they always were while the job
            // runs - unless this activation already counted this step's outcome, which must not count twice.
            if (_jobStepGrains?.ContainsKey(stepId) != true && _outcomesCounted.Contains(stepId))
            {
                _logger.IgnoringOutcomeOfEarlierExecution(stepId, reported);
                return true;
            }
        }

        return await IgnoreLateStepOutcome(stepId);
    }

    /// <summary>
    /// Remember that this activation counted a step's success or failure.
    /// </summary>
    /// <param name="stepId">The <see cref="JobStepId"/> of the step.</param>
    void OutcomeCounted(JobStepId stepId) => _outcomesCounted.Add(stepId);

    /// <summary>
    /// Counts the stopped steps of the current execution from the steps' own records, replacing what was counted.
    /// </summary>
    /// <param name="steps">The steps to count the stopped ones of.</param>
    /// <remarks>
    /// A stop is counted once per step, so a step counted here is not counted again when its own report arrives late.
    /// </remarks>
    void CountStopsFrom(IEnumerable<JobStepState> steps)
    {
        _stopsCounted.Clear();
        _stopsCounted.UnionWith(steps.Where(step => step.Status is JobStepStatus.Stopped).Select(step => step.Id.JobStepId));
        State.Progress.StoppedSteps = _stopsCounted.Count;
    }

    /// <summary>
    /// Forgets the stops counted so far, as an execution that begins anew has none.
    /// </summary>
    void ForgetCountedStops()
    {
        _stopsCounted.Clear();
        State.Progress.StoppedSteps = 0;
    }

    bool IsDuplicateOutcome(JobStepId stepId, int attempt) =>
        _acceptedOutcomes.Contains((stepId, attempt)) ||
        (_countsFromStorage && _jobStepGrains?.ContainsKey(stepId) != true && !_pendingRetries.ContainsKey(stepId));

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
