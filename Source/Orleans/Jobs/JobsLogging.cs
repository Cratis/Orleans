// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Orleans.Jobs;

internal static partial class JobsLogging
{
    [LoggerMessage(LogLevel.Warning, "Could not activate jobs manager {Key}; the job start does not depend on the manager")]
    internal static partial void ManagerActivationFailed(this ILogger<Jobs> logger, JobsManagerKey key, Exception exception);
}
