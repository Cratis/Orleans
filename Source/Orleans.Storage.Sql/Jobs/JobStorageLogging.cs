// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Orleans.Storage.Sql.Jobs;

internal static partial class JobStorageLogging
{
    [LoggerMessage(LogLevel.Warning, "Skipping stored job {JobId} because job type {JobType} is not registered")]
    internal static partial void SkippingUnknownJobType(this ILogger<JobStorage> logger, Guid jobId, JobType jobType);
}
