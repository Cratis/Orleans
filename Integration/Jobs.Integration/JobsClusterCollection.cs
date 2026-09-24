// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration;

/// <summary>
/// Puts every job-system integration spec in one collection, so they run one at a time.
/// </summary>
/// <remarks>
/// They share a silo and a database, and each resets storage before it runs. Left to run in parallel they
/// would wipe each other's jobs mid-flight and fail for reasons that have nothing to do with the job system.
/// </remarks>
[CollectionDefinition(Name, DisableParallelization = true)]
public static class JobsClusterCollection
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "JobsCluster";
}
