// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Represents the configuration for jobs.
/// </summary>
public class JobsOptions
{
    /// <summary>
    /// Gets the maximum number of parallel job steps that can be executed concurrently.
    /// </summary>
    /// <remarks>
    /// If not configured, defaults to the number of processor threads minus 1, but never less than 1.
    /// </remarks>
    public int? MaxParallelSteps { get; init; }

    /// <summary>
    /// Gets the time threshold for considering a job as dead in the water.
    /// Jobs in preparation state with no steps that were created before this threshold are candidates for cleanup.
    /// Defaults to 1 hour.
    /// </summary>
    public TimeSpan DeadJobThreshold { get; init; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets the cleanup cadence for the scavenger process that removes dead jobs.
    /// This determines how often the cleanup process runs to check for and remove jobs stuck in preparation.
    /// Defaults to 1 hour.
    /// </summary>
    public TimeSpan CleanupCadence { get; init; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets the number of successfully handled events after which a job step makes its progress checkpoint
    /// durable.
    /// </summary>
    /// <remarks>
    /// A job step reports its progress after every consecutive batch it hands off, and each report persisted
    /// the whole step document. That write is debounced: the checkpoint is made durable once this many batches
    /// have been reported, and any other state write (a status change on completion, failure, or stop) flushes
    /// the pending checkpoint too, as does <see cref="StepCheckpointFlushInterval"/> for a step that has gone
    /// idle mid-range. A step always resumes from its last persisted checkpoint and re-reads forward, so a
    /// crash between debounced writes re-delivers every batch handled since that checkpoint — a performer must
    /// tolerate being asked twice. A larger value trades a longer post-crash re-scan for fewer writes.
    /// Defaults to 100.
    /// </remarks>
    public int StepCheckpointBatchInterval { get; init; } = 100;

    /// <summary>
    /// Gets how long a job step may hold an unpersisted progress checkpoint before it is flushed regardless of
    /// how few batches have been reported.
    /// </summary>
    /// <remarks>
    /// <see cref="StepCheckpointBatchInterval"/> is a pure counter, so a step that hands off a few batches and
    /// then goes quiet — a slow performer, a sparse partition, a step that stops mid-range — would keep those
    /// batches unpersisted indefinitely and re-deliver all of them after a crash. This bounds that window in
    /// time. Defaults to 5 seconds; a value of zero or less disables the timed flush.
    /// </remarks>
    public TimeSpan StepCheckpointFlushInterval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets the maximum number of stored jobs resumed concurrently when the jobs manager rehydrates.
    /// Defaults to 8.
    /// </summary>
    /// <remarks>
    /// Every resumed job reads its state, activates, queries its steps and starts them, so an unbounded burst
    /// can exhaust the storage connection pool. Jobs are resumed oldest first. Values below 1 are treated as 1.
    /// </remarks>
    public int MaxConcurrentRehydration { get; init; } = 8;

    /// <summary>
    /// Gets the maximum number of dead jobs deleted concurrently by the cleanup process.
    /// Defaults to 4.
    /// </summary>
    /// <remarks>
    /// Values below 1 are treated as 1.
    /// </remarks>
    public int MaxConcurrentCleanup { get; init; } = 4;

    /// <summary>
    /// Gets how long immutable terminal jobs are retained before cleanup.
    /// </summary>
    /// <remarks>
    /// Completed-with-failures and failed jobs are retained for diagnostics, then removed with their regular and
    /// failed job steps. Preparing, running, stopped, and removing jobs are never retention candidates. Defaults
    /// to 7 days; set to <see cref="Timeout.InfiniteTimeSpan"/> to retain terminal jobs indefinitely.
    /// </remarks>
    public TimeSpan TerminalJobRetention { get; init; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Gets the maximum number of terminal jobs selected for one cleanup pass.
    /// </summary>
    /// <remarks>
    /// Cleanup fetches this bounded oldest-first page at storage rather than materializing the job collection.
    /// Values below 1 are treated as 1. Defaults to 100.
    /// </remarks>
    public int MaxTerminalJobsPerCleanup { get; init; } = 100;

    /// <summary>
    /// Gets the maximum number of job steps a single job starts concurrently when it starts or resumes.
    /// Defaults to 16. Values below 1 are treated as 1.
    /// </summary>
    public int MaxConcurrentStepStarts { get; init; } = 16;

    /// <summary>
    /// Gets the effective maximum parallel steps to use.
    /// </summary>
    /// <returns>The maximum parallel steps value.</returns>
    public int GetEffectiveMaxParallelSteps() => MaxParallelSteps ?? Math.Max(1, Environment.ProcessorCount - 1);
}
