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
    /// Gets the maximum number of job steps a single job keeps active at once, or null to start every step of the
    /// job (or of its current stage) up front.
    /// </summary>
    /// <remarks>
    /// With a limit the job starts that many steps and starts the next one each time a step reaches an outcome, so a
    /// job with thousands of steps does not activate them all at once. Steps that have not started yet stay in
    /// storage with their prepared state, and a job that resumes rebuilds its window from storage. A job can override
    /// the limit through <c language="csharp">Job.MaxActiveSteps</c>. Values below 1 are treated as 1. Defaults to null.
    /// </remarks>
    public int? MaxActiveStepsPerJob { get; init; }

    /// <summary>
    /// Gets how many times a job step is attempted before its failure counts as the job's failure.
    /// </summary>
    /// <remarks>
    /// An attempt that fails while more are left is scheduled again by the job after a backoff, instead of being
    /// counted as failed. A job can decide per failure through <c language="csharp">Job.ShouldRetryStep</c>. Values below 1 are
    /// treated as 1. Defaults to 1, which never retries.
    /// </remarks>
    public int MaxStepAttempts { get; init; } = 1;

    /// <summary>
    /// Gets the delay before the second attempt of a failed job step; every further attempt doubles it.
    /// Defaults to 5 seconds.
    /// </summary>
    public TimeSpan StepRetryBackoff { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets the longest delay between two attempts of a job step. Defaults to 5 minutes.
    /// </summary>
    public TimeSpan MaxStepRetryBackoff { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets how often a running job checks its steps in storage and finalizes itself when every step is done.
    /// </summary>
    /// <remarks>
    /// The counters a job keeps while steps report to it are a cache: a report that never arrived leaves the job
    /// running with every step finished. The watchdog counts the steps in storage, finalizes a job whose steps are
    /// all done and, when <see cref="StepLeaseTimeout"/> is on, restarts steps whose owner is gone. A zero, negative
    /// or infinite interval turns the watchdog off. Defaults to 1 minute.
    /// </remarks>
    public TimeSpan JobWatchdogInterval { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets how long a running job step may go without a heartbeat before the watchdog may restart it.
    /// </summary>
    /// <remarks>
    /// Off by default. When on, a running step records a lease - its silo, activation and last heartbeat - and renews
    /// it with every state write and on a timer. The watchdog restarts a step only when its owner silo is no longer
    /// part of the cluster and its lease has expired. A step without a lease, written by a release that did not know
    /// them, is left alone for one lease period after its last change and then started again, which is harmless for
    /// a step that is still running. A zero, negative or infinite value turns leases off.
    /// </remarks>
    public TimeSpan StepLeaseTimeout { get; init; } = TimeSpan.Zero;

    /// <summary>
    /// Gets how many times a job step delivers its outcome to its job before it leaves the delivery to the job's
    /// watchdog. Defaults to 10.
    /// </summary>
    /// <remarks>
    /// The outcome is persisted before it is delivered, so a delivery that never succeeds loses nothing: the step stays
    /// in the state it persisted and the job's watchdog accounts for it. Values below 1 are treated as 1. A step that is
    /// being removed is not limited by this: it reports its stop until the job has stored it, because it deletes itself
    /// once the job has.
    /// </remarks>
    public int StepDeliveryMaxAttempts { get; init; } = 10;

    /// <summary>
    /// Gets the delay before the second delivery of a step's outcome; every further delivery doubles it.
    /// Defaults to 1 second.
    /// </summary>
    public TimeSpan StepDeliveryRetryDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets the longest delay between two deliveries of a step's outcome. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan MaxStepDeliveryRetryDelay { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets how long stopping a job step waits for its work to settle.
    /// </summary>
    /// <remarks>
    /// A stopped step cancels its work and waits this long for the work and its activation callbacks to finish
    /// before it answers. A step that does not settle in time answers <see cref="JobStepError.StopTimedOut"/>, is
    /// not quiescent, and reports its stop once the work settles. Keep it below the Orleans response timeout so
    /// the caller sees that answer rather than a timed-out call. Deactivation is bounded by Orleans' own
    /// deactivation timeout instead. Defaults to 10 seconds.
    /// </remarks>
    public TimeSpan StepStopTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets the effective maximum parallel steps to use.
    /// </summary>
    /// <returns>The maximum parallel steps value.</returns>
    public int GetEffectiveMaxParallelSteps() => MaxParallelSteps ?? Math.Max(1, Environment.ProcessorCount - 1);
}
