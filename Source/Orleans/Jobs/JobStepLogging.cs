// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;
using Cratis.Orleans.Workers;
using Microsoft.Extensions.Logging;

namespace Cratis.Orleans.Jobs;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

public static partial class JobStepLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Starting job step")]
    public static partial void Starting(this ILogger<IJobStep> logger);

    [LoggerMessage(LogLevel.Warning, "Could not start job step because preparation failed with '{PrepareError}'")]
    public static partial void StartPrepareFailed(this ILogger<IJobStep> logger, JobStepError prepareError);

    [LoggerMessage(LogLevel.Debug, "Resuming job step")]
    public static partial void Resuming(this ILogger<IJobStep> logger);

    [LoggerMessage(LogLevel.Debug, "Pausing job step")]
    public static partial void Pausing(this ILogger<IJobStep> logger);

    [LoggerMessage(LogLevel.Debug, "Stopping job step")]
    public static partial void Stopping(this ILogger<IJobStep> logger);

    [LoggerMessage(LogLevel.Debug, "Job step is already stopped")]
    public static partial void AlreadyStopped(this ILogger<IJobStep> logger);

    [LoggerMessage(LogLevel.Debug, "Job step changing status to {Status}")]
    public static partial void ChangingStatus(this ILogger<IJobStep> logger, JobStepStatus status);

    [LoggerMessage(LogLevel.Warning, "Job step handling unexpected error from performing job step")]
    public static partial void HandleUnexpectedPerformJobStepFailure(this ILogger<IJobStep> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Job step failed with an unexpected error")]
    public static partial void FailedUnexpectedly(this ILogger<IJobStep> logger, Exception exception);

    [LoggerMessage(LogLevel.Debug, "Job step reporting failure performing work")]
    public static partial void ReportFailurePerformingWork(this ILogger<IJobStep> logger);

    [LoggerMessage(LogLevel.Warning, "Job step failed to report failure. This might cause unexpected behaviour")]
    public static partial void FailedToReportFailure(this ILogger<IJobStep> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Job step failed to write persisted state")]
    public static partial void FailedToWriteState(this ILogger<IJobStep> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Job step failed to write persisted state to change status to {JobStepStatus}")]
    public static partial void FailedToWriteState(this ILogger<IJobStep> logger, Exception exception, JobStepStatus jobStepStatus);

    [LoggerMessage(LogLevel.Warning, "Job step failed to report succeeded job step to job. {JobError}")]
    public static partial void FailedReportJobStepSuccess(this ILogger<IJobStep> logger, JobError jobError);

    [LoggerMessage(LogLevel.Warning, "Job step failed to report failed job step to job. {JobError}")]
    public static partial void FailedReportJobStepFailure(this ILogger<IJobStep> logger, JobError jobError);

    [LoggerMessage(LogLevel.Warning, "Job step while after performing work failed while persisting state. {JobStepError}")]
    public static partial void PerformingWorkFailedPersistState(this ILogger<IJobStep> logger, JobStepError jobStepError);

    [LoggerMessage(LogLevel.Warning, "Job step '{JobStepName}' failed while preparing")]
    public static partial void FailedPreparing(this ILogger<IJobStep> logger, Exception ex, string jobStepName);

    [LoggerMessage(LogLevel.Warning, "Job step '{JobStepName}' failed while performing job step")]
    public static partial void FailedPerforming(this ILogger<IJobStep> logger, Exception ex, string jobStepName);

    [LoggerMessage(LogLevel.Warning, "Failed to report that that the performing of job step was cancelled. Error: {ReportError}")]
    public static partial void FailedReportingJobStepCancelled(this ILogger<IJobStep> logger, PerformWorkError reportError);

    [LoggerMessage(LogLevel.Warning, "Job step '{JobStepName}' ({JobStepId}) of job {JobId} did not settle within {Elapsed} of being stopped; not settled: {Unsettled}. The step is not quiescent; its stop is reported once the work settles")]
    internal static partial void StopTimedOut(this ILogger<IJobStep> logger, string jobStepName, Guid jobStepId, Guid jobId, TimeSpan elapsed, string unsettled);

    [LoggerMessage(LogLevel.Warning, "Job step '{JobStepName}' ({JobStepId}) of job {JobId} did not settle within {Elapsed} of deactivating; not settled: {Unsettled}. Callbacks from its work are refused from now on")]
    internal static partial void DeactivationTimedOut(this ILogger<IJobStep> logger, string jobStepName, Guid jobStepId, Guid jobId, TimeSpan elapsed, string unsettled);

    [LoggerMessage(LogLevel.Debug, "Job step refused an activation callback from run {RunId}: {Reason}")]
    internal static partial void RejectedActivationCallback(this ILogger<IJobStep> logger, long runId, ActivationCallbackRejected reason);

    [LoggerMessage(LogLevel.Debug, "Job step skipped writing status {Status} for a run the activation no longer owns")]
    internal static partial void SkippedWriteForEndedRun(this ILogger<IJobStep> logger, JobStepStatus status);

    [LoggerMessage(LogLevel.Debug, "Job step skipped writing status {Status} for a run whose outcome a drain persisted")]
    internal static partial void SkippedWriteForDrainedRun(this ILogger<IJobStep> logger, JobStepStatus status);

    [LoggerMessage(LogLevel.Debug, "Job step skipped reporting an outcome a drain already handed to its job")]
    internal static partial void SkippedReportForDrainedRun(this ILogger<IJobStep> logger);

    [LoggerMessage(LogLevel.Information, "Job step was not drained within {Timeout}; its stop goes on")]
    internal static partial void NotDrainedInTime(this ILogger<IJobStep> logger, TimeSpan timeout);

    [LoggerMessage(LogLevel.Information, "Job step defers its start in execution {Generation} until the stop of its earlier run has settled")]
    internal static partial void DeferringStartUntilStopSettles(this ILogger<IJobStep> logger, long generation);

    [LoggerMessage(LogLevel.Information, "Job step ignored a stop of earlier execution {Generation}")]
    internal static partial void IgnoringStopOfEarlierExecution(this ILogger<IJobStep> logger, long generation);

    [LoggerMessage(LogLevel.Debug, "Job step did not need its deferred start: {Outcome}")]
    internal static partial void DeferredStartNotNeeded(this ILogger<IJobStep> logger, StartJobStepError outcome);

    [LoggerMessage(LogLevel.Warning, "Job step could not run its deferred start: {Error}")]
    internal static partial void DeferredStartFailed(this ILogger<IJobStep> logger, StartJobStepError error);

    [LoggerMessage(LogLevel.Information, "Removing step {JobStepId} whose parent job {JobId} no longer exists")]
    internal static partial void RemovingStepWithoutParentJob(this ILogger<IJobStep> logger, JobStepId jobStepId, JobId jobId);

    [LoggerMessage(LogLevel.Warning, "Could not report step stop: parent job {JobId} was not found")]
    internal static partial void ParentJobNotFound(this ILogger<IJobStep> logger, JobId jobId);

    [LoggerMessage(LogLevel.Warning, "Could not read parent job {JobId}: {Error}")]
    internal static partial void ParentJobReadFailed(this ILogger<IJobStep> logger, JobId jobId, Storage.Jobs.JobError error);

    [LoggerMessage(LogLevel.Warning, "Could not resolve parent job {JobId} of unavailable type {JobType}")]
    internal static partial void ParentJobTypeUnavailable(this ILogger<IJobStep> logger, JobId jobId, JobType jobType);

    [LoggerMessage(LogLevel.Warning, "Could not resolve parent job {JobId}")]
    internal static partial void ParentLookupFailed(this ILogger<IJobStep> logger, JobId jobId, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Parent job {JobId} lookup exceeded the step stop deadline")]
    internal static partial void ParentLookupTimedOut(this ILogger<IJobStep> logger, JobId jobId);

    [LoggerMessage(LogLevel.Debug, "Parent job {JobId} lookup ended after the step activation ended")]
    internal static partial void ParentLookupActivationEnded(this ILogger<IJobStep> logger, JobId jobId);

    [LoggerMessage(LogLevel.Warning, "Could not report step outcome: parent job {JobId} has not been resolved")]
    internal static partial void ParentNotResolved(this ILogger<IJobStep> logger, JobId jobId);

    [LoggerMessage(LogLevel.Information, "Running job step again as attempt {Attempt}")]
    internal static partial void RetryingAttempt(this ILogger<IJobStep> logger, int attempt);

    [LoggerMessage(LogLevel.Warning, "The job did not acknowledge the job step outcome on delivery {Attempt}: {JobError}")]
    internal static partial void FailedReportJobStepOutcome(this ILogger<IJobStep> logger, int attempt, JobError jobError);

    [LoggerMessage(LogLevel.Warning, "Delivering the job step outcome to the job failed on delivery {Attempt}")]
    internal static partial void FailedDeliveringJobStepOutcome(this ILogger<IJobStep> logger, Exception exception, int attempt);

    [LoggerMessage(LogLevel.Warning, "The job did not acknowledge the job step outcome after {Attempts} deliveries; the persisted outcome is left for the job's watchdog")]
    internal static partial void GaveUpDeliveringJobStepOutcome(this ILogger<IJobStep> logger, int attempts);
}

public static class JobStepScopes
{
    public static IDisposable? BeginJobStepScope(this ILogger<IJobStep> logger, JobStepState state) =>
        logger.BeginScope(new
        {
            state.Id.JobId,
            state.Id.JobStepId,
            JobStepName = state.Name,
            JobStepStatus = state.Status
        });
    public static IDisposable? BeginJobStepScope(this ILogger<IJobStep> logger, JobStepIdentifier jobStepIdentifier, JobStepName jobStepName, JobStepStatus jobStepStatus = JobStepStatus.Unknown) =>
        logger.BeginScope(new
        {
            jobStepIdentifier.JobId,
            jobStepIdentifier.JobStepId,
            JobStepName = jobStepName,
            JobStepStatus = jobStepStatus
        });
}
