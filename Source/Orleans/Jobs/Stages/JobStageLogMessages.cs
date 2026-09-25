// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Orleans.Jobs.Stages;

#pragma warning disable SA1600 // Elements should be documented

internal static partial class JobStageLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Starting stage {Stage} with {StepCount} steps")]
    internal static partial void StartingStage(this ILogger<IJob> logger, JobStepStage stage, int stepCount);

    [LoggerMessage(LogLevel.Warning, "Stage {Stage} had {FailedSteps} failed steps - stopping the job and recording the steps in every later stage as unreachable")]
    internal static partial void StageFailed(this ILogger<IJob> logger, JobStepStage stage, int failedSteps);

    [LoggerMessage(LogLevel.Warning, "Job failed recording job step {JobStepId} as {JobStepStatus}. Error: {Error}")]
    internal static partial void FailedRecordingJobStepStatus(this ILogger<IJob> logger, JobStepId jobStepId, JobStepStatus jobStepStatus, JobStepError error);

    [LoggerMessage(LogLevel.Warning, "Job failed recording job step {JobStepId} as {JobStepStatus}")]
    internal static partial void FailedRecordingJobStepStatus(this ILogger<IJob> logger, Exception ex, JobStepId jobStepId, JobStepStatus jobStepStatus);
}
