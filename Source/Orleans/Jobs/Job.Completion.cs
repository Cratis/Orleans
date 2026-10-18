// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    IGrainTimer? _completionRetryTimer;
    TimeSpan _completionRetryDelay = TimeSpan.FromSeconds(1);
    bool _completionStateWritten;
    bool _clearCompletedState;
    bool _completionFinalized;
    int _completionClearAttempts;

    async Task<Result<HandleCompletionSuccess, JobError>> FinalizeCompletion()
    {
        if (!_completionStateWritten)
        {
            if ((await WriteState()).TryGetException(out _))
            {
                ScheduleCompletionRetry();
                return JobError.PersistStateError;
            }
            _completionStateWritten = true;
            _completionRetryDelay = TimeSpan.FromSeconds(1);
        }

        var cleared = false;
        if (_clearCompletedState)
        {
            try
            {
                _completionClearAttempts++;
                await ClearStateAsync();
                cleared = true;
            }
            catch (Exception exception)
            {
                _logger.FailedClearingCompletedState(exception, _completionClearAttempts);
                if (_completionClearAttempts < 2)
                {
                    ScheduleCompletionRetry();
                    return JobError.PersistStateError;
                }

                // The terminal status is already durable; retention can remove any record left behind.
            }
        }

        _completionFinalized = true;
        DeactivateOnIdleDuringShutdown();
        return cleared ? HandleCompletionSuccess.ClearedState : HandleCompletionSuccess.NotClearedState;
    }

    void ScheduleCompletionRetry()
    {
        _completionRetryTimer = this.RegisterGrainTimer(
            async _ =>
            {
                _completionRetryTimer?.Dispose();
                _completionRetryTimer = null;
                using var scope = _logger.BeginJobScope(JobId, JobKey);
                await FinalizeCompletion();
            },
            new GrainTimerCreationOptions
            {
                DueTime = _completionRetryDelay,
                Period = Timeout.InfiniteTimeSpan,
                KeepAlive = true
            });
        _completionRetryDelay = TimeSpan.FromSeconds(Math.Min(_completionRetryDelay.TotalSeconds * 2, 60));
    }

    void ResetCompletionFinalization()
    {
        // Retrying failed steps starts a new execution; an old clear must never delete its state.
        _completionRetryTimer?.Dispose();
        _completionRetryTimer = null;
        _completionRetryDelay = TimeSpan.FromSeconds(1);
        _completionStateWritten = false;
        _clearCompletedState = false;
        _completionFinalized = false;
        _completionClearAttempts = 0;
    }
}
