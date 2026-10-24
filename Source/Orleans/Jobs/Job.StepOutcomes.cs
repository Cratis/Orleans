// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    /// <summary>
    /// The number of recorded outcomes at which to warn about stored state size, without limiting the record.
    /// </summary>
    internal const int CountedOutcomesWarningThreshold = 100_000;

    readonly HashSet<JobStepId> _stepsToRestartAfterEarlierStop = [];
    readonly HashSet<JobStepId> _outcomesCounted = [];
    HashSet<JobStepId> _recordedOutcomes = [];
    IList<JobStepId>? _recordedOutcomesSource;
    bool _warnedAboutCountedOutcomes;
    IGrainTimer? _restartStepsTimer;

    /// <summary>
    /// Decides whether a reported step outcome is ignored, reading the generation the report carries.
    /// </summary>
    /// <param name="stepId">The <see cref="JobStepId"/> of the reporting step.</param>
    /// <param name="stopped">Whether the step reports that it stopped.</param>
    /// <returns>Whether the outcome is ignored, and the attempt it was reported for.</returns>
    async Task<(bool Ignored, int Attempt, bool OfEarlierExecution, JobError? Unsaved)> IgnoreStepOutcome(JobStepId stepId, bool stopped)
    {
        // Read and removed first, so neither the generation nor the attempt flows into the hooks and stages the outcome
        // leads to. Only a generation the step reported counts: one it merely inherited from its job's call says nothing
        // about its run.
        var attempt = JobStepAttempt.TakeReported();
        var generation = JobExecutionGeneration.TakeReported();
        var ofEarlierExecution = generation is { } reported && reported < ExecutionGeneration();
        var (ignored, unsaved) = await IgnoreStepOutcome(stepId, stopped, attempt, generation);
        return (ignored, attempt, ofEarlierExecution, unsaved);
    }

    async Task<(bool Ignored, JobError? Unsaved)> IgnoreStepOutcome(JobStepId stepId, bool stopped, int attempt, long? generation)
    {
        // A drain took this step's outcome from the step itself; the step's own report of it counts nothing.
        if (_accountedByDrain.Contains(stepId))
        {
            _logger.IgnoringOutcomeAccountedForByDrain(stepId);
            return (true, null);
        }

        // A step delivers its outcome until it is acknowledged, so the same outcome can arrive again: once for each
        // attempt is enough, and what storage already accounted for after a restart is not counted a second time.
        if (!stopped && IsDuplicateOutcome(stepId, attempt))
        {
            _logger.IgnoringDuplicateOutcome(stepId, attempt);
            return (true, null);
        }

        if (generation is { } reported && reported < ExecutionGeneration())
        {
            // A stop of an earlier execution is not a stop of this one: the step stays tracked and is started again.
            // While the job is removed there is no execution left to start it in again, so its stop is counted -
            // once, as any other - for the removal to finish.
            if (stopped && State.Status is not JobStatus.Removing)
            {
                _logger.IgnoringStopOfEarlierExecution(stepId, reported);
                RestartAfterEarlierStop(stepId);
                return (true, null);
            }

            // Success and failure are facts whatever the execution, and are counted as they always were while the job
            // runs - unless this step's outcome was already counted, in this activation or durably before it.
            if (_jobStepGrains?.ContainsKey(stepId) != true && OutcomeWasCounted(stepId))
            {
                return (true, await SaveCountedOutcomeIfNeeded(stepId, reported));
            }
        }

        return (await IgnoreLateStepOutcome(stepId), null);
    }

    async Task<JobError?> SaveCountedOutcomeIfNeeded(JobStepId stepId, long generation)
    {
        if (_stateMayBeUnsaved && (await WriteState()).TryGetException(out _))
        {
            _logger.CountedOutcomeNotSaved(stepId);
            return JobError.PersistStateError;
        }
        _logger.IgnoringOutcomeOfEarlierExecution(stepId, generation);
        return null;
    }

    /// <summary>
    /// Remember a counted outcome, persisting it for executions that began with a resume or reopen.
    /// </summary>
    /// <param name="stepId">The <see cref="JobStepId"/> of the step.</param>
    void OutcomeCounted(JobStepId stepId)
    {
        _outcomesCounted.Add(stepId);
        if (!RecordsCountedOutcomes() || !RecordedOutcomes().Add(stepId))
        {
            return;
        }
        State.CompletedStepsCounted.Add(stepId);
        if (!_warnedAboutCountedOutcomes && State.CompletedStepsCounted.Count >= CountedOutcomesWarningThreshold)
        {
            _warnedAboutCountedOutcomes = true;
            _logger.ManyCountedOutcomesRecorded(State.CompletedStepsCounted.Count);
        }
    }

    bool RecordsCountedOutcomes()
    {
        var generation = ExecutionGeneration();
        return generation > 0 && State.StatusChanges[(int)generation - 1].Status is JobStatus.Running;
    }

    HashSet<JobStepId> RecordedOutcomes()
    {
        if (!ReferenceEquals(_recordedOutcomesSource, State.CompletedStepsCounted))
        {
            _recordedOutcomesSource = State.CompletedStepsCounted;
            _recordedOutcomes = [.. _recordedOutcomesSource];
        }
        return _recordedOutcomes;
    }

    bool OutcomeWasCounted(JobStepId stepId) => _outcomesCounted.Contains(stepId) || RecordedOutcomes().Contains(stepId);

    void UncountFailures(IReadOnlyCollection<JobStepState> failed)
    {
        State.Progress.FailedSteps = Math.Max(0, State.Progress.FailedSteps - failed.Count);
        foreach (var step in failed)
        {
            var stepId = step.Id.JobStepId;
            _outcomesCounted.Remove(stepId);
            RecordedOutcomes().Remove(stepId);
            State.CompletedStepsCounted.Remove(stepId);
        }
    }

    /// <summary>
    /// Counts the stopped steps of the current execution from the steps' own records, replacing what was counted.
    /// </summary>
    /// <param name="steps">The steps to count the stopped ones of.</param>
    /// <remarks>
    /// A stop is counted once per step, so a step counted here is not counted again when its own report arrives late.
    /// </remarks>
    void CountStopsFrom(IEnumerable<JobStepState> steps)
    {
        State.StoppedStepsCounted = steps
            .Where(step => step.Status is JobStepStatus.Stopped)
            .Select(step => step.Id.JobStepId)
            .Distinct()
            .ToList();
        State.Progress.StoppedSteps = State.StoppedStepsCounted.Count;
    }

    /// <summary>
    /// Forgets the stops counted so far, as an execution that begins anew has none.
    /// </summary>
    /// <remarks>
    /// A new list rather than clearing the current one, so a copy kept to restore an execution stays as it was.
    /// </remarks>
    void ForgetCountedStops()
    {
        State.StoppedStepsCounted = [];
        State.Progress.StoppedSteps = 0;
    }

    /// <summary>
    /// Counts a step's stop, unless the current execution already counted it.
    /// </summary>
    /// <param name="stepId">The <see cref="JobStepId"/> of the step.</param>
    /// <returns>True if the stop was counted now; false if it was counted before.</returns>
    /// <remarks>
    /// The steps counted are saved with the job, together with the count, so a stop is counted once per execution
    /// even when its report arrives again in a later activation.
    /// </remarks>
    bool CountStop(JobStepId stepId)
    {
        if (State.StoppedStepsCounted.Contains(stepId))
        {
            return false;
        }
        State.StoppedStepsCounted.Add(stepId);
        State.Progress.StoppedSteps++;
        return true;
    }

    /// <summary>
    /// Takes back a stop the removal counted for a step whose success or failure arrives after it.
    /// </summary>
    /// <param name="stepId">The <see cref="JobStepId"/> of the step.</param>
    /// <remarks>
    /// While a job is removed, the stop of a step's earlier execution is counted. The step's run of the current
    /// execution may still succeed or fail, and that outcome is counted as well, so the step would count twice. The
    /// outcome wins: the stop is taken back, and the same write saves the count and the steps counted.
    /// </remarks>
    void UncountStopDuringRemoval(JobStepId stepId)
    {
        if (State.Status is not JobStatus.Removing || !State.StoppedStepsCounted.Remove(stepId))
        {
            return;
        }
        State.Progress.StoppedSteps--;
        _logger.UncountingStopOfEarlierExecution(stepId);
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
