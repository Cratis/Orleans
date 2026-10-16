// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Represents one run of a job step on one activation: its identity, its work, the callbacks the work has on the
/// activation, its single outcome and the delivery of that outcome.
/// </summary>
/// <remarks>
/// Admission, worker settlement and delivery settlement are separate states. The phase only decides who owns the
/// outcome. The outcome and delivery ownership are only changed under the step's gate.
/// </remarks>
/// <param name="id">The identity of the run within its activation.</param>
/// <param name="scheduler">The activation's <see cref="TaskScheduler"/>, captured when the run was created.</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Work that outlived a timed-out stop may still observe the tokens; they hold no timers or wait handles to free.")]
internal sealed class JobStepRun(long id, TaskScheduler scheduler)
{
    static readonly AsyncLocal<JobStepRun?> _current = new();

    readonly object _admission = new();
    readonly CancellationTokenSource _cancellation = new();
    readonly TaskCompletionSource _startup = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly CancellationTokenSource _admissionClosed = new();
    int _admittedCallbacks;
    TaskCompletionSource? _callbacksSettled;

    /// <summary>
    /// Gets or sets the run the current background work belongs to. Set before the work is launched, so the work
    /// carries its own run and never reads the activation's current run.
    /// </summary>
    public static JobStepRun? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    /// <summary>
    /// Gets the identity of the run within its activation.
    /// </summary>
    public long Id { get; } = id;

    /// <summary>
    /// Gets the activation's <see cref="TaskScheduler"/>.
    /// </summary>
    public TaskScheduler Scheduler { get; } = scheduler;

    /// <summary>
    /// Gets the token the run's work is cancelled through.
    /// </summary>
    public CancellationToken Cancellation => _cancellation.Token;

    /// <summary>
    /// Gets the phase of the run.
    /// </summary>
    public JobStepRunPhase Phase { get; private set; } = JobStepRunPhase.Starting;

    /// <summary>
    /// Gets a value indicating whether a stop or deactivation has begun draining the run.
    /// </summary>
    public bool IsDraining => Phase >= JobStepRunPhase.Stopping;

    /// <summary>
    /// Gets a value indicating whether the run still admits activation callbacks.
    /// </summary>
    public bool AdmissionOpen => !_admissionClosed.IsCancellationRequested;

    /// <summary>
    /// Gets the token that is cancelled once the run stops admitting activation callbacks.
    /// </summary>
    public CancellationToken AdmissionClosed => _admissionClosed.Token;

    /// <summary>
    /// Gets the task that completes once starting the run has finished, whether or not work was launched.
    /// </summary>
    public Task Startup => _startup.Task;

    /// <summary>
    /// Gets or sets the whole background task of the run's work.
    /// </summary>
    public Task WorkerSettled { get; set; } = Task.CompletedTask;

    /// <summary>
    /// Gets or sets the task delivering the run's outcome.
    /// </summary>
    public Task DeliverySettled { get; set; } = Task.CompletedTask;

    /// <summary>
    /// Gets the outcome of the run, once recorded. It is recorded once and never replaced.
    /// </summary>
    public JobStepResult? Outcome { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the delivery of the outcome has been claimed.
    /// </summary>
    public bool DeliveryClaimed { get; private set; }

    /// <summary>
    /// Gets a task that completes when no admitted callback is running.
    /// </summary>
    public Task CallbacksSettled
    {
        get
        {
            lock (_admission)
            {
                return _callbacksSettled?.Task ?? Task.CompletedTask;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether nothing of the run is still in flight: starting, work, callbacks and
    /// delivery have all finished.
    /// </summary>
    public bool IsSettled => Startup.IsCompleted && WorkerSettled.IsCompleted && DeliverySettled.IsCompleted && CallbacksSettled.IsCompleted;

    /// <summary>
    /// Records that starting the run has finished.
    /// </summary>
    public void Started() => _startup.TrySetResult();

    /// <summary>
    /// Records that the run's work has been launched and owns its outcome.
    /// </summary>
    public void Running()
    {
        if (Phase is JobStepRunPhase.Starting)
        {
            Phase = JobStepRunPhase.Running;
        }
    }

    /// <summary>
    /// Begins draining the run: the drainer takes ownership of the outcome and the work is asked to cancel. Does
    /// not wait for anything. A run already draining keeps its reason, except that a stop becomes a removal.
    /// </summary>
    /// <param name="phase">The draining <see cref="JobStepRunPhase"/>.</param>
    public void BeginDrain(JobStepRunPhase phase)
    {
        if (IsDraining)
        {
            if (Phase is JobStepRunPhase.Stopping && phase is JobStepRunPhase.Removing)
            {
                Phase = phase;
            }
            return;
        }

        Phase = phase;

        // Cancellation is requested synchronously; registered callbacks run on the thread pool.
        _ = _cancellation.CancelAsync();
    }

    /// <summary>
    /// Records the outcome of the run, unless one is already recorded.
    /// </summary>
    /// <param name="outcome">The <see cref="JobStepResult"/>.</param>
    /// <returns>True if recorded, false if the run already has an outcome.</returns>
    public bool TryRecord(JobStepResult outcome)
    {
        if (Outcome is not null)
        {
            return false;
        }

        Outcome = outcome;
        return true;
    }

    /// <summary>
    /// Claims the delivery of the outcome, unless it is already claimed.
    /// </summary>
    /// <returns>True if claimed, false if someone else delivers the outcome.</returns>
    public bool TryClaimDelivery()
    {
        if (DeliveryClaimed)
        {
            return false;
        }

        DeliveryClaimed = true;
        return true;
    }

    /// <summary>
    /// Closes admission: callbacks that have not started are refused from now on.
    /// </summary>
    public void CloseAdmission()
    {
        lock (_admission)
        {
            if (!_admissionClosed.IsCancellationRequested)
            {
                // Callbacks registered on the token only refuse queued callbacks; they never block.
                _admissionClosed.Cancel();
            }
        }
    }

    /// <summary>
    /// Admits a callback that is about to run, unless admission is closed.
    /// </summary>
    /// <returns>True if admitted; the caller must call <see cref="Leave"/> when the callback is done.</returns>
    public bool TryAdmit()
    {
        lock (_admission)
        {
            if (!AdmissionOpen)
            {
                return false;
            }

            if (_admittedCallbacks++ == 0)
            {
                _callbacksSettled = new(TaskCreationOptions.RunContinuationsAsynchronously);
            }
            return true;
        }
    }

    /// <summary>
    /// Records that an admitted callback is done.
    /// </summary>
    public void Leave()
    {
        lock (_admission)
        {
            if (--_admittedCallbacks == 0)
            {
                _callbacksSettled!.TrySetResult();
                _callbacksSettled = null;
            }
        }
    }

    /// <summary>
    /// Describes what of the run has not settled, for a timed-out stop or deactivation.
    /// </summary>
    /// <returns>A description of the unsettled parts.</returns>
    public string DescribeUnsettled()
    {
        var unsettled = new List<string>();
        if (!Startup.IsCompleted)
        {
            unsettled.Add("startup");
        }
        if (!WorkerSettled.IsCompleted)
        {
            unsettled.Add("worker");
        }
        if (!CallbacksSettled.IsCompleted)
        {
            unsettled.Add("activation callbacks");
        }
        return string.Join(", ", unsettled);
    }
}
