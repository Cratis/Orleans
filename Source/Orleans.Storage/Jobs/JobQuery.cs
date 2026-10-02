// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Orleans.Storage.Jobs;

/// <summary>
/// Describes one bounded page of jobs to retrieve from storage.
/// </summary>
public sealed record JobQuery
{
    /// <summary>
    /// Gets the optional job type to include.
    /// </summary>
    public JobType? Type { get; init; }

    /// <summary>
    /// Gets the job statuses to include. An empty collection includes every status.
    /// </summary>
    public IReadOnlyCollection<JobStatus> Statuses { get; init; } = [];

    /// <summary>
    /// Gets the exclusive creation-time cutoff for jobs to include.
    /// </summary>
    public DateTimeOffset? CreatedBefore { get; init; }

    /// <summary>
    /// Gets the number of matching jobs to skip.
    /// </summary>
    public int Skip { get; init; }

    /// <summary>
    /// Gets the maximum number of matching jobs to return.
    /// </summary>
    public int Take { get; init; } = 100;
}
