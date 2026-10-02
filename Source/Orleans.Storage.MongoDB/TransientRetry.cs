// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;

namespace Cratis.Orleans.Storage.MongoDB;

/// <summary>
/// Retries MongoDB operations that fail with a transient error, using exponential backoff with jitter.
/// </summary>
/// <remarks>
/// Only <see cref="MongoWaitQueueFullException"/>, <see cref="TimeoutException"/>, <see cref="MongoConnectionException"/>
/// and <see cref="MongoExecutionTimeoutException"/> are retried; everything else surfaces immediately. When the
/// attempts are exhausted the original exception is rethrown.
/// </remarks>
/// <param name="retryCount">The number of retries after the first attempt. Zero disables retrying.</param>
/// <param name="baseDelay">The delay before the first retry; it doubles for every following retry.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/> used to wait between attempts.</param>
/// <param name="logger">The <see cref="ILogger"/> to log retries to.</param>
/// <param name="jitter">Optional source of values in [0, 1) used to randomize delays; defaults to <see cref="Random.Shared"/>.</param>
public class TransientRetry(
    int retryCount,
    TimeSpan baseDelay,
    TimeProvider? timeProvider = default,
    ILogger? logger = default,
    Func<double>? jitter = default)
{
    readonly int _retryCount = Math.Max(0, retryCount);
    readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    readonly ILogger _logger = logger ?? NullLogger.Instance;
    readonly Func<double> _jitter = jitter ?? Random.Shared.NextDouble;

    /// <summary>
    /// Gets a <see cref="TransientRetry"/> with the default options.
    /// </summary>
    public static TransientRetry Default { get; } = new(new MongoDBJobsStorageOptions().TransientRetryCount, new MongoDBJobsStorageOptions().TransientRetryBaseDelay);

    /// <summary>
    /// Gets whether an exception represents a transient storage error worth retrying.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to inspect.</param>
    /// <returns>True if transient, false if not.</returns>
    public static bool IsTransient(Exception exception) =>
        exception is MongoWaitQueueFullException or TimeoutException or MongoConnectionException or MongoExecutionTimeoutException;

    /// <summary>
    /// Execute an operation, retrying it when it fails with a transient error.
    /// </summary>
    /// <typeparam name="TResult">The result type of the operation.</typeparam>
    /// <param name="operationName">Description of the operation, used for logging.</param>
    /// <param name="operation">The operation to execute.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<TResult> Execute<TResult>(string operationName, Func<Task<TResult>> operation)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                return await operation().ConfigureAwait(false);
            }
            catch (Exception ex) when (IsTransient(ex))
            {
                attempt++;
                if (attempt > _retryCount)
                {
                    _logger.GivingUpAfterTransientErrors(operationName, attempt, ex);
                    throw;
                }

                var delay = DelayFor(attempt);
                _logger.RetryingAfterTransientError(operationName, attempt, _retryCount, delay, ex);
                await Task.Delay(delay, _timeProvider).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Execute an operation that has no result, retrying it when it fails with a transient error.
    /// </summary>
    /// <param name="operationName">Description of the operation, used for logging.</param>
    /// <param name="operation">The operation to execute.</param>
    /// <returns>Awaitable task.</returns>
    public Task Execute(string operationName, Func<Task> operation) =>
        Execute(operationName, async () =>
        {
            await operation().ConfigureAwait(false);
            return true;
        });

    TimeSpan DelayFor(int attempt)
    {
        var exponential = baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1);

        // Between 50% and 100% of the exponential delay, so concurrent callers do not retry in lockstep.
        return TimeSpan.FromMilliseconds(exponential * (0.5 + (_jitter() * 0.5)));
    }
}
