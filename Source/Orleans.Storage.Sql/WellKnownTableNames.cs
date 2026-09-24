// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Storage.Sql;

/// <summary>
/// Holds the well known table names used by the SQL jobs storage.
/// </summary>
public static class WellKnownTableNames
{
    /// <summary>
    /// The table holding jobs.
    /// </summary>
    public const string Jobs = "Jobs";

    /// <summary>
    /// The table holding job steps.
    /// </summary>
    /// <remarks>
    /// Failed job steps live in this table alongside every other step and are told apart by their status,
    /// unlike the MongoDB provider which moves them into a dedicated collection.
    /// </remarks>
    public const string JobSteps = "JobSteps";
}
