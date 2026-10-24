// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    IGrainTimer? _removalRetryTimer;
    TimeSpan _removalRetryDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Schedules another attempt at finishing a removal that every step has reported for, but that could not be finished.
    /// </summary>
    /// <remarks>
    /// A one-shot timer that keeps the activation alive, from one second doubling up to a minute, like a completion retry.
    /// Its callback is an ordinary grain turn, so it never interleaves with a step's report that finishes the removal.
    /// Only one is armed at a time; a timer belongs to its activation and goes away with it, after which rehydration
    /// takes the removal over.
    /// The retry is also kept armed while a step that is accounted for still has to be told about the removal.
    /// </remarks>
    void ScheduleRemovalRetry()
    {
        if (State.Status is not JobStatus.Removing || _removalRetryTimer is not null)
        {
            return;
        }

        _logger.SchedulingRemovalFinalizationRetry(_removalRetryDelay);
        _removalRetryTimer = this.RegisterGrainTimer(
            RetryRemoval,
            new GrainTimerCreationOptions
            {
                DueTime = _removalRetryDelay,
                Period = Timeout.InfiniteTimeSpan,
                KeepAlive = true
            });
        _removalRetryDelay = TimeSpan.FromSeconds(Math.Min(_removalRetryDelay.TotalSeconds * 2, 60));
    }

    async Task RetryRemoval(CancellationToken cancellationToken)
    {
        _removalRetryTimer?.Dispose();
        _removalRetryTimer = null;
        if (State.Status is not JobStatus.Removing)
        {
            ClearRemovalRetry();
            return;
        }

        using var scope = _logger.BeginJobScope(JobId, JobKey);
        var result = await StopNonCompletedStepsAndEnsureCompletion(true);
        if (result is null && State.Progress is { IsCompleted: true } or { IsStopped: true })
        {
            ScheduleRemovalRetry();
        }
    }

    void ClearRemovalRetry()
    {
        _removalRetryTimer?.Dispose();
        _removalRetryTimer = null;
        _removalRetryDelay = TimeSpan.FromSeconds(1);
    }
}
