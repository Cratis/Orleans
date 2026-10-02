// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Cratis.Orleans.Storage.MongoDB.Jobs;

/// <summary>
/// An observable job envelope that leaves the request and remaining state as BSON until its type is checked.
/// </summary>
internal sealed class StoredJob
{
    /// <summary>
    /// Gets or sets the identifier used for observation membership.
    /// </summary>
    public JobId Id { get; set; } = JobId.NotSet;

    /// <summary>
    /// Gets or sets the persisted job type name.
    /// </summary>
    public JobType Type { get; set; } = JobType.NotSet;

    /// <summary>
    /// Gets or sets the materialized status.
    /// </summary>
    public JobStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the creation time used for sorting.
    /// </summary>
    public DateTimeOffset Created { get; set; }

    /// <summary>
    /// Gets or sets the remaining state without interpreting its request.
    /// </summary>
    [BsonExtraElements]
    public BsonDocument State { get; set; } = [];
}
