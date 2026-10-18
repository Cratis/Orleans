// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    readonly HashSet<JobStepId> _failedStepNotifications = [];
    IGrainTimer? _stepNotificationRetryTimer;
    TimeSpan _stepNotificationRetryDelay = TimeSpan.FromSeconds(1);

    Task NotifySteps(bool removing) => NotifySteps(_observers!.Observers, removing);

    async Task NotifySteps(IEnumerable<KeyValuePair<JobStepId, IJobObserver>> observers, bool removing)
    {
        // A failed call must not evict its observer: the step still owes this execution an outcome.
        using var generation = JobExecutionGeneration.Carry(ExecutionGeneration());
        foreach (var (stepId, observer) in observers.ToArray())
        {
            try
            {
                if (removing)
                {
                    await observer.OnJobRemoved();
                }
                else
                {
                    await observer.OnJobStopped();
                }
                _failedStepNotifications.Remove(stepId);
            }
            catch (Exception ex)
            {
                _logger.FailedNotifyingJobStep(ex, stepId, removing);
                _failedStepNotifications.Add(stepId);
            }
        }
        ArmStepNotificationRetry();
    }

    async Task RetryStepNotifications(CancellationToken cancellationToken)
    {
        _stepNotificationRetryTimer?.Dispose();
        _stepNotificationRetryTimer = null;
        if (State.Status is not (JobStatus.Stopped or JobStatus.Removing))
        {
            ClearStepNotificationRetries();
            return;
        }

        using var scope = _logger.BeginJobScope(JobId, JobKey);
        var observers = _observers!.Observers;
        _failedStepNotifications.IntersectWith(observers.Keys);
        await NotifySteps(observers.Where(observer => _failedStepNotifications.Contains(observer.Key)), State.Status is JobStatus.Removing);
    }

    void ArmStepNotificationRetry()
    {
        if (_failedStepNotifications.Count == 0 || State.Status is not (JobStatus.Stopped or JobStatus.Removing))
        {
            ClearStepNotificationRetries();
            return;
        }
        if (_stepNotificationRetryTimer is not null)
        {
            return;
        }

        _stepNotificationRetryTimer = this.RegisterGrainTimer(
            RetryStepNotifications,
            new GrainTimerCreationOptions { DueTime = _stepNotificationRetryDelay, Period = Timeout.InfiniteTimeSpan });
        _stepNotificationRetryDelay = TimeSpan.FromSeconds(Math.Min(_stepNotificationRetryDelay.TotalSeconds * 2, 60));
    }

    void ForgetStepNotification(JobStepId stepId)
    {
        _failedStepNotifications.Remove(stepId);
        if (_failedStepNotifications.Count == 0)
        {
            ClearStepNotificationRetries();
        }
    }

    void ClearStepNotificationRetries()
    {
        _stepNotificationRetryTimer?.Dispose();
        _stepNotificationRetryTimer = null;
        _failedStepNotifications.Clear();
        _stepNotificationRetryDelay = TimeSpan.FromSeconds(1);
    }
}
