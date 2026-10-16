// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Orleans.Jobs;

internal static partial class JobsRecoveryLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Jobs recovery is incomplete: only manager keys seen on this silo are known. Provide a singleton IJobsManagerKeys implementation enumerating all host scopes and namespaces for complete recovery.")]
    internal static partial void IncompleteRecoveryKeys(this ILogger<JobsRecovery> logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to resolve or enumerate jobs manager keys from {Provider}")]
    internal static partial void KeyProviderFailed(this ILogger<JobsRecovery> logger, Exception exception, Type provider);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recovering jobs after {Count} cluster members became dead")]
    internal static partial void RecoveringAfterMemberFailure(this ILogger<JobsRecovery> logger, int count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Requested recovery for {Count} jobs managers")]
    internal static partial void RecoveredManagers(this ILogger<JobsRecovery> logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to recover jobs for {Key}")]
    internal static partial void ManagerRecoveryFailed(this ILogger<JobsRecovery> logger, Exception exception, JobsManagerKey key);

    [LoggerMessage(Level = LogLevel.Error, Message = "Jobs recovery participant failed")]
    internal static partial void RecoveryFailed(this ILogger<JobsRecovery> logger, Exception exception);
}
