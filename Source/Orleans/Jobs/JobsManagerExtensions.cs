// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Extension method for <see cref="IJobsManager"/>.
/// </summary>
public static class JobsManagerExtensions
{
    /// <summary>
    /// Gets the <see cref="IJobsManager"/> grain.
    /// </summary>
    /// <param name="factory">The <see cref="IGrainFactory"/>.</param>
    /// <param name="scope">The storage scope to get the jobs manager for.</param>
    /// <param name="namespace">The namespace within the scope to get the jobs manager for.</param>
    /// <returns>The <see cref="IJobsManager"/> grain.</returns>
    public static IJobsManager GetJobsManager(this IGrainFactory factory, string scope, string @namespace)
        => factory.GetGrain<IJobsManager>(0, new JobsManagerKey(scope, @namespace));
}
