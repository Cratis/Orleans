// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    readonly Dictionary<JobStepId, (int Attempt, DateTimeOffset Due)> _pendingRetries = [];
    readonly Dictionary<JobStepId, int> _attemptsToRun = [];
    readonly HashSet<(JobStepId StepId, int Attempt)> _acceptedOutcomes = [];
    IGrainTimer? _retryTimer;

    /// <summary>
    /// Gets a value indicating whether the failed steps of this job can be run again after the job completed with failures.
    /// </summary>
    /// <remarks>
    /// Off by default. Resuming a job that completed with failures never runs its failed steps; a job type that wants that
    /// opts in here and is asked through <see cref="IJobsManager.RetryFailedSteps"/>.
    /// </remarks>
    protected virtual bool CanRetryFailedSteps => false;

    /// <inheritdoc/>
    public async Task<Result<int, RetryFailedStepsError>> RetryFailed()
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        try
        {
            if (!CanRetryFailedSteps || IsStaged)
            {
                return Result.Failed<int, RetryFailedStepsError>(RetryFailedStepsError.NotSupported);
            }
            if (State.Status is not JobStatus.CompletedWithFailures)
            {
                return Result.Failed<int, RetryFailedStepsError>(RetryFailedStepsError.JobIsNotCompletedWithFailures);
            }

            // The in-memory terminal status must be durable before another execution may replace it.
            if (_completionRetryTimer is not null && !_completionStateWritten)
            {
                return Result.Failed<int, RetryFailedStepsError>(RetryFailedStepsError.CouldNotPersist);
            }

            var getFailed = await Storage.JobSteps.GetForJob(JobId, JobStepStatus.CompletedWithFailure);
            getFailed.RethrowError();
            var failed = getFailed.AsT0;
            if (failed.Count == 0)
            {
                return Result.Success<int, RetryFailedStepsError>(0);
            }

            var progress = State.Progress with { };
            var written = await WriteOrUndo(() =>
            {
                State.Progress.FailedSteps = Math.Max(0, State.Progress.FailedSteps - failed.Count);
                StatusChanged(JobStatus.Running);
            });
            if (written.TryGetException(out _))
            {
                State.Progress = progress;
                DeactivateOnIdleDuringShutdown();
                return Result.Failed<int, RetryFailedStepsError>(RetryFailedStepsError.CouldNotPersist);
            }

            _logger.RetryingFailedSteps(failed.Count);
            _jobStepGrains ??= [];
            _started.Clear();
            _inFlight.Clear();
            foreach (var step in failed)
            {
                var stepId = step.Id.JobStepId;
                _jobStepGrains[stepId] = GetJobStepGrain(step);
                _attemptsToRun[stepId] = step.Attempt + 1;
                _acceptedOutcomes.RemoveWhere(_ => _.StepId == stepId);
            }
            TrackStagesOf(failed);
            _countsFromStorage = false;

            var window = SelectWindow(failed.ToDictionary(step => step.Id.JobStepId, step => _jobStepGrains[step.Id.JobStepId]));
            _ = await StartAndSubscribeToJobSteps(this.GetGrainId(), window);
            return Result.Success<int, RetryFailedStepsError>(failed.Count);
        }
        catch (Exception ex)
        {
            _logger.Failed(ex);
            return Result.Failed<int, RetryFailedStepsError>(RetryFailedStepsError.Unknown);
        }
    }

    /// <summary>
    /// Runs the steps whose backoff has passed as their next attempt.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    internal async Task RetryDueSteps()
    {
        _retryTimer?.Dispose();
        _retryTimer = null;
        if (State.Status is not (JobStatus.Running or JobStatus.StartingSteps))
        {
            await FlushPendingRetriesAsFailures();
            return;
        }

        using var scope = _logger.BeginJobScope(JobId, JobKey);
        var now = DateTimeOffset.UtcNow;
        var due = _pendingRetries.Where(_ => _.Value.Due <= now).ToList();
        foreach (var (stepId, (attempt, _)) in due)
        {
            _pendingRetries.Remove(stepId);
            if (_jobStepGrains?.TryGetValue(stepId, out var step) != true)
            {
                continue;
            }

            Result<StartJobStepError> started;
            try
            {
                using (JobExecutionGeneration.Carry(ExecutionGeneration()))
                {
                    started = await step!.Retry(this.GetGrainId(), attempt);
                }
            }
            catch (Exception ex)
            {
                _logger.FailedStartingJobStep(ex, stepId);
                await CountFailureOfRetryThatCouldNotStart(stepId);
                continue;
            }

            if (!started.TryGetError(out var error))
            {
                continue;
            }
            if (error is StartJobStepError.AlreadyStarted)
            {
                // The step is still delivering the failed attempt; ask again shortly.
                _pendingRetries[stepId] = (attempt, DateTimeOffset.UtcNow + TimeSpan.FromSeconds(1));
                continue;
            }

            _logger.FailedStartingJobStep(stepId, error);
            await CountFailureOfRetryThatCouldNotStart(stepId);
        }

        ArmRetryTimer();
    }

    /// <summary>
    /// Decides whether a step that failed an attempt is run again.
    /// </summary>
    /// <param name="stepId">The <see cref="JobStepId"/> of the step.</param>
    /// <param name="result">The <see cref="JobStepResult"/> of the failed attempt.</param>
    /// <param name="attempt">The number of the attempt that failed, starting at 1.</param>
    /// <returns>True to run the step again after <see cref="GetStepRetryDelay"/>; false to count the failure.</returns>
    /// <remarks>
    /// Defaults to retrying while <paramref name="attempt"/> is below <see cref="JobsOptions.MaxStepAttempts"/>, which
    /// is 1 unless configured, so a step is not retried. A job overrides it to tell a failure worth retrying - a
    /// timeout - from one that is not.
    /// </remarks>
    protected virtual bool ShouldRetryStep(JobStepId stepId, JobStepResult result, int attempt) =>
        attempt < Math.Max(1, JobsOptionsOrDefault.MaxStepAttempts);

    /// <summary>
    /// Gets how long to wait before the attempt that follows a failed one.
    /// </summary>
    /// <param name="failedAttempt">The number of the attempt that failed, starting at 1.</param>
    /// <returns>The delay, which doubles with every attempt up to <see cref="JobsOptions.MaxStepRetryBackoff"/>.</returns>
    protected virtual TimeSpan GetStepRetryDelay(int failedAttempt)
    {
        var options = JobsOptionsOrDefault;
        var factor = Math.Pow(2, Math.Max(0, failedAttempt - 1));
        var ticks = options.StepRetryBackoff.Ticks * factor;
        return TimeSpan.FromTicks((long)Math.Min(ticks, Math.Max(options.MaxStepRetryBackoff.Ticks, options.StepRetryBackoff.Ticks)));
    }

    bool DecidesToRetry(JobStepId stepId, JobStepResult result, int attempt)
    {
        if (State.Status is not (JobStatus.Running or JobStatus.StartingSteps) || _jobStepGrains?.ContainsKey(stepId) != true)
        {
            return false;
        }

        try
        {
            return ShouldRetryStep(stepId, result, attempt);
        }
        catch (Exception ex)
        {
            _logger.Failed(ex);
            return false;
        }
    }

    void ScheduleStepRetry(JobStepId stepId, int failedAttempt)
    {
        var delay = GetStepRetryDelay(failedAttempt);
        _logger.SchedulingStepRetry(stepId, failedAttempt + 1, delay);
        _pendingRetries[stepId] = (failedAttempt + 1, DateTimeOffset.UtcNow + delay);
        ArmRetryTimer();
    }

    void ArmRetryTimer()
    {
        _retryTimer?.Dispose();
        _retryTimer = null;
        if (_pendingRetries.Count == 0)
        {
            return;
        }

        var next = _pendingRetries.Values.Min(_ => _.Due);
        var dueIn = next - DateTimeOffset.UtcNow;
        _retryTimer = this.RegisterGrainTimer(
            _ => RetryDueSteps(),
            new GrainTimerCreationOptions
            {
                DueTime = dueIn > TimeSpan.Zero ? dueIn : TimeSpan.Zero,
                Period = Timeout.InfiniteTimeSpan
            });
    }

    async Task CountFailureOfRetryThatCouldNotStart(JobStepId stepId)
    {
        OutcomeCounted(stepId);
        State.Progress.FailedSteps++;
        CountStepOutcomeInStage(stepId, succeeded: false);
        _ = await PerformStepEventHandling(stepId, JobStepResult.Failed("The step could not be run again after it failed."));
    }

    /// <summary>
    /// Counts the failures that were waiting for their next attempt as final, as a stop or removal ends the wait.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    async Task FlushPendingRetriesAsFailures()
    {
        _retryTimer?.Dispose();
        _retryTimer = null;
        foreach (var stepId in _pendingRetries.Keys.ToList())
        {
            _pendingRetries.Remove(stepId);
            OutcomeCounted(stepId);
            State.Progress.FailedSteps++;
            CountStepOutcomeInStage(stepId, succeeded: false);
            await UnsubscribeJobStep(stepId);
            _jobStepGrains?.Remove(stepId);
            LeaveWindow(stepId);
        }
    }
}
