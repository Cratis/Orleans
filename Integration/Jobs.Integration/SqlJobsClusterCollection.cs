// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.Integration;

/// <summary>
/// Keeps the SQL-backed specs on one silo and out of each other's way.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public static class SqlJobsClusterCollection
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "sql-jobs-cluster";
}
