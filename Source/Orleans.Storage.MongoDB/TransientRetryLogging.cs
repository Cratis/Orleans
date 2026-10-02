// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Orleans.Storage.MongoDB;

internal static partial class TransientRetryLogging
{
    [LoggerMessage(LogLevel.Warning, "Transient storage error during {Operation}; retry {Attempt} of {MaxRetries} in {Delay}")]
    internal static partial void RetryingAfterTransientError(this ILogger logger, string operation, int attempt, int maxRetries, TimeSpan delay, Exception exception);

    [LoggerMessage(LogLevel.Error, "Giving up on {Operation} after {Attempts} attempts because of transient storage errors")]
    internal static partial void GivingUpAfterTransientErrors(this ILogger logger, string operation, int attempts, Exception exception);
}
