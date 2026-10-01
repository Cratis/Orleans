// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Orleans.Storage.MongoDB.Jobs;

internal static partial class JobStepStorageLogging
{
    [LoggerMessage(LogLevel.Warning, "Skipping stored job step {JobStepId} because job step type {JobStepType} cannot be resolved")]
    internal static partial void SkippingUnknownJobStepType(this ILogger<JobStepStorage> logger, string jobStepId, JobStepType jobStepType);
}
