// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Orleans.Jobs;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

public static partial class JobsManagerLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Rehydrating jobs system")]
    public static partial void Rehydrating(this ILogger<JobsManager> logger);

    [LoggerMessage(LogLevel.Debug, "Starting job {JobId}")]
    public static partial void StartingJob(this ILogger<JobsManager> logger, JobId jobId);

    [LoggerMessage(LogLevel.Debug, "Resuming job {JobId}")]
    public static partial void ResumingJob(this ILogger<JobsManager> logger, JobId jobId);

    [LoggerMessage(LogLevel.Warning, "Error while resuming job {JobId}")]
    public static partial void ErrorResumingJob(this ILogger<JobsManager> logger, Exception ex, JobId jobId);

    [LoggerMessage(LogLevel.Debug, "Stopping job {JobId}")]
    public static partial void StoppingJob(this ILogger<JobsManager> logger, JobId jobId);

    [LoggerMessage(LogLevel.Debug, "Deleting job {JobId}")]
    public static partial void DeletingJob(this ILogger<JobsManager> logger, JobId jobId);

    [LoggerMessage(LogLevel.Debug, "Job {JobId} completed with status {Status}")]
    public static partial void JobCompleted(this ILogger<JobsManager> logger, JobId jobId, JobStatus status);

    [LoggerMessage(LogLevel.Warning, "An unknown error occurred in Job {JobId}")]
    public static partial void UnknownError(this ILogger<JobsManager> logger, Exception exception, JobId jobId);

    [LoggerMessage(LogLevel.Warning, "An unknown error occurred")]
    public static partial void UnknownError(this ILogger<JobsManager> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Job {JobId} could not be found")]
    public static partial void JobCouldNotBeFound(this ILogger<JobsManager> logger, JobId jobId);

    [LoggerMessage(LogLevel.Debug, "Job {JobId} cannot be removed or stopped because it is completed")]
    public static partial void JobIsCompletedAndCannotBeRemovedOrStopped(this ILogger<JobsManager> logger, JobId jobId);

    [LoggerMessage(LogLevel.Debug, "Job {JobId} is already being removed")]
    public static partial void JobIsAlreadyBeingRemoved(this ILogger<JobsManager> logger, JobId jobId);

    [LoggerMessage(LogLevel.Warning, "Job {JobId} is cannot be stopped because it is not running")]
    public static partial void JobCannotBeStoppedIsNotRunning(this ILogger<JobsManager> logger, JobId jobId);

    [LoggerMessage(LogLevel.Warning, "Job {JobId} failed to be removed")]
    public static partial void FailedToRemoveJob(this ILogger<JobsManager> logger, JobId jobId);

    [LoggerMessage(LogLevel.Warning, "Job {JobId} an error occurred while performing action. {JobError}")]
    public static partial void JobErrorOccurred(this ILogger<JobsManager> logger, JobId jobId, JobError jobError);

    [LoggerMessage(LogLevel.Warning, "Job {JobId} an error occurred while resuming job steps. {JobSteps}")]
    public static partial void FailedToResumeJobSteps(this ILogger<JobsManager> logger, JobId jobId, IEnumerable<JobStepId> jobSteps);

    [LoggerMessage(LogLevel.Debug, "Job {JobId} cannot be resumed because it is running")]
    public static partial void CannotResumeJobBecauseAlreadyRunning(this ILogger<JobsManager> logger, JobId jobId);

    [LoggerMessage(LogLevel.Debug, "Job {JobId} cannot be resumed because it is completed")]
    public static partial void CannotResumeJobBecauseCompleted(this ILogger<JobsManager> logger, JobId jobId);

    [LoggerMessage(LogLevel.Warning, "Job {JobId} encountered error : {Error}")]
    public static partial void JobErrorOccurred(this ILogger<JobsManager> logger, JobId jobId, Storage.Jobs.JobError error);

    [LoggerMessage(LogLevel.Warning, "Unable to get jobs of type {JobType}. Encountered error : {Error}")]
    public static partial void UnableToGetJobs(this ILogger<JobsManager> logger, Type jobType, Storage.Jobs.JobError error);

    [LoggerMessage(LogLevel.Warning, "Unable to get job grain {JobId} for job type {JobType}. Error {Error}")]
    public static partial void UnableToGetJob(this ILogger<JobsManager> logger, JobId jobId, JobType jobType, IJobTypes.GetClrTypeForError error);

    [LoggerMessage(LogLevel.Warning, "Unable to get all jobs. Encountered error")]
    public static partial void UnableToGetAllJobs(this ILogger<JobsManager> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Failed to stop Job {JobId}")]
    public static partial void FailedToStopJob(this ILogger<JobsManager> logger, JobId jobId);

    [LoggerMessage(LogLevel.Warning, "Failed to delete Job {JobId}")]
    public static partial void FailedToDeleteJob(this ILogger<JobsManager> logger, JobId jobId);

    [LoggerMessage(LogLevel.Warning, "Could not resume Job {JobId} because it is not prepared yet")]
    public static partial void CannotResumeUnpreparedJob(this ILogger<JobsManager> logger, JobId jobId);

    [LoggerMessage(LogLevel.Warning, "Job {JobId} cannot be resumed")]
    public static partial void JobCannotBeResumed(this ILogger<JobsManager> logger, JobId jobId);

    [LoggerMessage(LogLevel.Warning, "Failed to resume Job {JobId}")]
    public static partial void FailedResumingJob(this ILogger<JobsManager> logger, JobId jobId);

    [LoggerMessage(LogLevel.Information, "Cleaning up dead jobs")]
    public static partial void CleaningUpDeadJobs(this ILogger<JobsManager> logger);

    [LoggerMessage(LogLevel.Information, "Found {Count} dead jobs to clean up")]
    public static partial void FoundDeadJobs(this ILogger<JobsManager> logger, int count);

    [LoggerMessage(LogLevel.Debug, "No dead jobs found")]
    public static partial void NoDeadJobsFound(this ILogger<JobsManager> logger);

    [LoggerMessage(LogLevel.Warning, "Failed to get jobs for cleanup")]
    public static partial void FailedToGetJobsForCleanup(this ILogger<JobsManager> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Failed to get step count for job {JobId}")]
    public static partial void FailedToGetStepCountForJob(this ILogger<JobsManager> logger, JobId jobId, Exception error);

    [LoggerMessage(LogLevel.Information, "Skipping job {JobId} due to step count retrieval error")]
    public static partial void SkippingJobDueToStepCountError(this ILogger<JobsManager> logger, JobId jobId);
}

public static class JobsManagerScopes
{
    public static IDisposable? BeginJobsManagerScope(this ILogger<JobsManager> logger, JobsManagerKey key) =>
        logger.BeginScope(new
        {
            key.Scope,
            key.Namespace
        });
}
