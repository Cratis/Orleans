// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Storage.MongoDB;

/// <summary>
/// Represents the options for <see cref="MongoDBJobsStorage"/>.
/// </summary>
public class MongoDBJobsStorageOptions
{
    /// <summary>
    /// Gets the function that resolves the database name for a scope and namespace.
    /// </summary>
    /// <remarks>
    /// When not set, <see cref="DatabaseNames.ForJobs"/> decides. Provide a resolver to match an existing
    /// naming scheme — Chronicle, for instance, maps its event store and namespace onto the same names its own
    /// storage has always used, so existing job data stays where it is.
    /// </remarks>
    public Func<string, string, string>? DatabaseNameResolver { get; set; }

    /// <summary>
    /// Gets or sets how many times a storage operation is retried after failing with a transient error
    /// (wait queue full, timeout, connection failure). Defaults to 5; zero disables retrying.
    /// </summary>
    public int TransientRetryCount { get; set; } = 5;

    /// <summary>
    /// Gets or sets the delay before the first retry of a transient failure. It doubles for every
    /// following retry and is randomized to avoid synchronized retries. Defaults to 200 milliseconds.
    /// </summary>
    public TimeSpan TransientRetryBaseDelay { get; set; } = TimeSpan.FromMilliseconds(200);
}
