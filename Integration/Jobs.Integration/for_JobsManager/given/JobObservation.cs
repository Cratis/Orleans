// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration.for_JobsManager.given;

/// <summary>
/// Waits for a job to reach a state, by reading the storage the job system writes to.
/// </summary>
/// <remarks>
/// The job system persists progress asynchronously, so a scenario that acts and then reads immediately is
/// racing it. These wait for the state the scenario is about, and fail with a deadline rather than hanging
/// or sleeping for a duration that happens to work on one machine.
/// </remarks>
public static class JobObservation
{
    /// <summary>
    /// The longest any of these will wait before giving up.
    /// </summary>
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Waits until the job satisfies a predicate.
    /// </summary>
    /// <param name="storage">The storage to read from.</param>
    /// <param name="jobId">The job to read.</param>
    /// <param name="predicate">The state being waited for.</param>
    /// <param name="timeout">How long to wait before giving up. Defaults to <see cref="Timeout"/>.</param>
    /// <returns>The job state that satisfied the predicate.</returns>
    /// <exception cref="TimeoutException">Thrown when the job did not reach the state in time.</exception>
    public static async Task<JobState> WaitTillJobMeetsPredicate(this IJobStorage storage, JobId jobId, Func<JobState, bool> predicate, TimeSpan? timeout = null)
    {
        JobState? last = null;
        using var cancellationTokenSource = new CancellationTokenSource(timeout ?? Timeout);
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            last = await storage.TryGetJob(jobId);
            if (last is not null && predicate(last))
            {
                return last;
            }

            await Task.Delay(50).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }

        throw new TimeoutException($"The job '{jobId}' never reached the expected state. Last seen: {(last is null ? "no job" : last.Status.ToString())}.");
    }

    /// <summary>
    /// Waits until the job reports its progress as completed.
    /// </summary>
    /// <param name="storage">The storage to read from.</param>
    /// <param name="jobId">The job to read.</param>
    /// <returns>The completed job state.</returns>
    public static Task<JobState> WaitTillJobProgressCompleted(this IJobStorage storage, JobId jobId) =>
        storage.WaitTillJobMeetsPredicate(jobId, state => state.Progress.IsCompleted);

    /// <summary>
    /// Waits until the job reports its progress as stopped.
    /// </summary>
    /// <param name="storage">The storage to read from.</param>
    /// <param name="jobId">The job to read.</param>
    /// <returns>The stopped job state.</returns>
    public static Task<JobState> WaitTillJobProgressStopped(this IJobStorage storage, JobId jobId) =>
        storage.WaitTillJobMeetsPredicate(jobId, state => state.Progress.IsStopped);

    /// <summary>
    /// Waits until the job is gone from storage.
    /// </summary>
    /// <param name="storage">The storage to read from.</param>
    /// <param name="jobId">The job to read.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="TimeoutException">Thrown when the job was still there when the deadline passed.</exception>
    public static async Task WaitTillJobIsDeleted(this IJobStorage storage, JobId jobId)
    {
        using var cancellationTokenSource = new CancellationTokenSource(Timeout);
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            if (await storage.TryGetJob(jobId) is null)
            {
                return;
            }

            await Task.Delay(50).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }

        throw new TimeoutException($"The job '{jobId}' was still in storage.");
    }

    /// <summary>
    /// Reads a job, or null when it is not there.
    /// </summary>
    /// <param name="storage">The storage to read from.</param>
    /// <param name="jobId">The job to read.</param>
    /// <returns>The job state, or null.</returns>
    public static async Task<JobState?> TryGetJob(this IJobStorage storage, JobId jobId)
    {
        var result = await storage.GetJob(jobId);
        return result.TryGetResult(out var state) ? state : null;
    }

    /// <summary>
    /// Reads every job step for a job, whether it succeeded, failed or was stopped.
    /// </summary>
    /// <param name="storage">The storage to read from.</param>
    /// <param name="jobId">The job whose steps to read.</param>
    /// <returns>The job steps.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the steps could not be read.</exception>
    public static async Task<IImmutableList<JobStepState>> GetJobSteps(this IJobStepStorage storage, JobId jobId)
    {
        var result = await storage.GetForJob(jobId);
        return result.Match(
            steps => steps,
            exception => throw new InvalidOperationException($"Reading the job steps for '{jobId}' failed.", exception));
    }
}
