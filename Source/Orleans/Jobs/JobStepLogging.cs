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
