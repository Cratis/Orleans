// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Orleans.Jobs;

namespace Cratis.Orleans.Storage;

/// <summary>
/// Represents an <see cref="IJobsStorage"/> implementation holding everything in memory - for testing and
/// for hosts that do not need jobs to survive a restart.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InMemoryJobsStorage"/> class.
/// </remarks>
/// <param name="jobTypes"><see cref="IJobTypes"/> that knows about the job type associations.</param>
public class InMemoryJobsStorage(IJobTypes jobTypes) : IJobsStorage
{
    readonly ConcurrentDictionary<string, JobsStorage> _storageByScopeAndNamespace = new();

    /// <inheritdoc/>
    public JobsStorage GetFor(string scope, string @namespace) =>
        _storageByScopeAndNamespace.GetOrAdd(
            string.Join('#', scope, @namespace),
            static (_, jobTypes) => new JobsStorage(new InMemory.JobStorage(jobTypes), new InMemory.JobStepStorage()),
            jobTypes);

    /// <inheritdoc/>
    /// <remarks>
    /// Here in-memory data itself lives in the storage instances, so forgetting them empties the store as well.
    /// </remarks>
    public void Reset() => _storageByScopeAndNamespace.Clear();
}
