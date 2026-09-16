// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobStepThrottle"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JobStepThrottle"/> class.
/// </remarks>
/// <param name="options">The <see cref="JobsOptions"/>.</param>
/// <param name="logger">Logger for logging.</param>
public class JobStepThrottle(IOptions<JobsOptions> options, ILogger<JobStepThrottle> logger) : IJobStepThrottle, IDisposable
{
    readonly SemaphoreSlim _semaphore = CreateSemaphore(GetEffectiveParallelism(options.Value));

    /// <inheritdoc/>
    public Task AcquireAsync(CancellationToken cancellationToken = default)
    {
        logger.AcquiringJobStepSlot();
        return _semaphore.WaitAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public void Release()
    {
        logger.ReleasingJobStepSlot();
        _semaphore.Release();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _semaphore.Dispose();
    }

    /// <summary>
    /// Gets the effective parallelism limit for job steps.
    /// </summary>
    /// <param name="options">The <see cref="JobsOptions"/>.</param>
    /// <returns>The effective parallelism limit.</returns>
    static int GetEffectiveParallelism(JobsOptions options) =>
        Math.Max(1, options.GetEffectiveMaxParallelSteps());

    static SemaphoreSlim CreateSemaphore(int maxParallelSteps) => new(maxParallelSteps, maxParallelSteps);
}
