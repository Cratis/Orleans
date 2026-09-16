// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Orleans.Jobs;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

public static partial class JobLogMessages
{
    [LoggerMessage(LogLevel.Warning, "An error occurred while writing state")]
    public static partial void FailedWritingState(this ILogger<IJob> logger, Exception ex);

    [LoggerMessage(LogLevel.Debug, "Starting job")]
    public static partial void Starting(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Debug, "Resuming job")]
    public static partial void Resuming(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Warning, "Job was left running with all of its steps already completed - finalizing it instead of resuming")]
    public static partial void FinalizingJobLeftRunningAfterAllStepsCompleted(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Warning, "Job failed recounting its progress from its job steps")]
    public static partial void FailedReconcilingProgressFromJobSteps(this ILogger<IJob> logger, Exception ex);

    [LoggerMessage(LogLevel.Debug, "Stopping job")]
    public static partial void Stopping(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Debug, "Removing job")]
    public static partial void Removing(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Debug, "Job changing status to {Status}")]
    public static partial void ChangingStatus(this ILogger<IJob> logger, JobStatus status);

    [LoggerMessage(LogLevel.Debug, "There are no prepared job steps. Completing job")]
    public static partial void NoJobStepsToStart(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Debug, "Found {JobStepsCount} job steps to prepare and start")]
    public static partial void PreparingJobSteps(this ILogger<IJob> logger, int jobStepsCount);

    [LoggerMessage(LogLevel.Debug, "An error occurred while preparing and starting job step {JobStepId}")]
    public static partial void ErrorPreparingJobStep(this ILogger<IJob> logger, Exception ex, JobStepId jobStepId);

    [LoggerMessage(LogLevel.Warning, "An error occurred while preparing and starting job steps")]
    public static partial void ErrorPreparingJobSteps(this ILogger<IJob> logger, Exception ex);

    [LoggerMessage(LogLevel.Warning, "Failed starting the job steps in the background with error {Error}")]
    public static partial void FailedStartingJobStepsInBackground(this ILogger<IJob> logger, StartJobError error);

    [LoggerMessage(LogLevel.Debug, "Not starting the job steps because the job is no longer preparing, it is {Status}")]
    public static partial void NotStartingJobStepsForJobThatIsNoLongerPreparing(this ILogger<IJob> logger, JobStatus status);

    [LoggerMessage(LogLevel.Trace, "Step {JobStepId} successfully completed")]
    public static partial void StepSuccessfullyCompleted(this ILogger<IJob> logger, JobStepId jobStepId);

    [LoggerMessage(LogLevel.Trace, "Step {JobStepId} stopped")]
    public static partial void StepStopped(this ILogger<IJob> logger, JobStepId jobStepId);

    [LoggerMessage(LogLevel.Trace, "Step {JobStepId} failed")]
    public static partial void StepFailed(this ILogger<IJob> logger, JobStepId jobStepId);

    [LoggerMessage(LogLevel.Debug, "Step {JobStepId} is no longer tracked by the job, skipping unsubscribe")]
    public static partial void CompletedJobStepIsNoLongerTracked(this ILogger<IJob> logger, JobStepId jobStepId);

    [LoggerMessage(LogLevel.Trace, "Preparing job steps for running")]
    public static partial void PrepareJobStepsForRunning(this ILogger<IJob> logger);

    [LoggerMessage(LogLevel.Warning, "Job failed")]
    public static partial void Failed(this ILogger<IJob> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Job failed to get job steps")]
    public static partial void FailedToGetJobSteps(this ILogger<IJob> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Job failed to persist new total steps count")]
    public static partial void FailedToSetTotalSteps(this ILogger<IJob> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Job failed to persist new job status {Status}")]
    public static partial void FailedWritingStatusChange(this ILogger<IJob> logger, JobStatus status);

    [LoggerMessage(LogLevel.Warning, "Job failed to persist new updated successful steps {SuccessfulStepsCount}")]
    public static partial void FailedUpdatingSuccessfulSteps(this ILogger<IJob> logger, Exception ex, int successfulStepsCount);

    [LoggerMessage(LogLevel.Warning, "Job failed to persist new updated failed steps {FailedStepsCount}")]
    public static partial void FailedUpdatingFailedSteps(this ILogger<IJob> logger, Exception ex, int failedStepsCount);

    [LoggerMessage(LogLevel.Warning, "Job failed remove state for all job steps and the job itself")]
    public static partial void FailedToRemoveForJob(this ILogger<IJob> logger, Exception error);

    [LoggerMessage(LogLevel.Warning, "Job failed on completed. Error: {JobError}")]
    public static partial void FailedOnCompleted(this ILogger<IJob> logger, JobError jobError);

    [LoggerMessage(LogLevel.Warning, "Job failed on completed")]
    public static partial void FailedOnCompleted(this ILogger<IJob> logger, Exception error);

    [LoggerMessage(LogLevel.Warning, "Job {JobId} {JobType} failed unexpectedly on OnAllStepsCompleted")]
    public static partial void FailedOnCompleted(this ILogger<IJob> logger, Exception ex, JobId jobId, JobType jobType);

    [LoggerMessage(LogLevel.Warning, "Job failed on completed while there are no job steps to run, but Job will still clear state. Error: {JobError}")]
    public static partial void FailedOnCompletedWhileNoJobSteps(this ILogger<IJob> logger, JobError jobError);

    [LoggerMessage(LogLevel.Debug, "Job is not in a running state. Status: {JobStatus}")]
    public static partial void JobIsNotRunning(this ILogger<IJob> logger, JobStatus jobStatus);

    [LoggerMessage(LogLevel.Warning, "Job failed handling completed job step {JobStepId}. Error: {JobError}")]
    public static partial void FailedHandlingCompletedJobStep(this ILogger<IJob> logger, JobStepId jobStepId, JobError jobError);

    [LoggerMessage(LogLevel.Warning, "Job failed persisting state after handling job step completion for job step {JobStepId}")]
    public static partial void FailedUpdatingStateAfterHandlingJobStepCompletion(this ILogger<IJob> logger, Exception ex, JobStepId jobStepId);

    [LoggerMessage(LogLevel.Warning, "Job failed persisting state after handling completion")]
    public static partial void FailedUpdatingStateAfterHandlingCompletion(this ILogger<IJob> logger, Exception ex);

    [LoggerMessage(LogLevel.Warning, "Job failed preparing job step {JobStepId}. Error: {Error}")]
    public static partial void FailedPreparingJobStep(this ILogger<IJob> logger, JobStepId jobStepId, PrepareJobStepError error);

    [LoggerMessage(LogLevel.Warning, "Job failed starting job step {JobStepId}. Error: {Error}")]
    public static partial void FailedStartingJobStep(this ILogger<IJob> logger, JobStepId jobStepId, StartJobStepError error);

    [LoggerMessage(LogLevel.Warning, "Job failed starting job step {JobStepId}")]
    public static partial void FailedStartingJobStep(this ILogger<IJob> logger, Exception ex, JobStepId jobStepId);

    [LoggerMessage(LogLevel.Debug, "Job step {JobStepId} was already started. Outcome: {Outcome}")]
    public static partial void JobStepWasAlreadyStarted(this ILogger<IJob> logger, JobStepId jobStepId, StartJobStepError outcome);

    [LoggerMessage(LogLevel.Warning, "Job failed subscribing to job step {JobStepId}")]
    public static partial void FailedSubscribingToJobStep(this ILogger<IJob> logger, Exception ex, JobStepId jobStepId);

    [LoggerMessage(LogLevel.Warning, "Job failed recording job step {JobStepId} as failed. Error: {Error}")]
    public static partial void FailedRecordingJobStepAsFailed(this ILogger<IJob> logger, JobStepId jobStepId, JobStepError error);

    [LoggerMessage(LogLevel.Warning, "Job failed recording job step {JobStepId} as failed")]
    public static partial void FailedRecordingJobStepAsFailed(this ILogger<IJob> logger, Exception ex, JobStepId jobStepId);

    [LoggerMessage(LogLevel.Debug, "Not all steps was completed successfully")]
    public static partial void AllStepsNotCompletedSuccessfully(this ILogger<IJob> logger);
}

public static class JobScopes
{
    public static IDisposable? BeginJobScope(this ILogger<IJob> logger, JobId jobId, JobKey key) =>
        logger.BeginScope(new
        {
            JobId = jobId,
            key.Scope,
            key.Namespace
        });
}
