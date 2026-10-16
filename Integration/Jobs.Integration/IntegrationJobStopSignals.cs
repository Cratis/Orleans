// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Orleans.Workers;

namespace Cratis.Orleans.Jobs.Integration;

/// <summary>
/// Holds one job's work before PerformStep and observes its stop callback without timing-based waits.
/// </summary>
public class IntegrationJobStopSignals
{
    static readonly ConcurrentDictionary<JobId, IntegrationJobStopSignals> _jobs = new();

    /// <summary>
    /// Gets the signal that scheduled work has entered the background task.
    /// </summary>
    public TaskCompletionSource WorkEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the gate that lets the work enter the job step's implementation.
    /// </summary>
    public TaskCompletionSource ContinueWork { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the signal that PerformStep was called.
    /// </summary>
    public TaskCompletionSource Performed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the signal that the job's OnStopped callback ran.
    /// </summary>
    public TaskCompletionSource Stopped { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the signal that the job completed all its steps.
    /// </summary>
    public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the work's outcome once the background work has returned. The step reports and persists its outcome
    /// after this, see <see cref="StepDeactivating"/>.
    /// </summary>
    public TaskCompletionSource<PerformWorkResult<JobStepResult>> WorkCompleted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the signal that the step began deactivating, which it asks for once its outcome is reported and
    /// persisted.
    /// </summary>
    public TaskCompletionSource StepDeactivating { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Registers signals for the job the specification is about to start.
    /// </summary>
    /// <param name="jobId">The job to observe.</param>
    /// <returns>The signals.</returns>
    public static IntegrationJobStopSignals Register(JobId jobId)
    {
        var signals = new IntegrationJobStopSignals();
        _jobs[jobId] = signals;
        return signals;
    }

    /// <summary>
    /// Finds the signals for a job, or none for a job that is not being held open.
    /// </summary>
    /// <param name="jobId">The job to observe.</param>
    /// <returns>The signals, if registered.</returns>
    public static IntegrationJobStopSignals? Find(JobId jobId) => _jobs.GetValueOrDefault(jobId);

    /// <summary>
    /// Removes a specification's signals after its job has finished reporting.
    /// </summary>
    /// <param name="jobId">The job no longer being observed.</param>
    public static void Remove(JobId jobId) => _jobs.TryRemove(jobId, out _);
}
