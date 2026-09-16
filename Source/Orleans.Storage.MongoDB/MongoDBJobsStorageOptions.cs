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
}
