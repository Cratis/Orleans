// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Cratis.Orleans.Storage.MongoDB.Jobs;

/// <summary>
/// Carries the raw job document through the MongoDB observer without resolving its request type.
/// </summary>
/// <remarks>
/// The base state retains the identity and sortable metadata the observer uses. Only the outgoing
/// snapshot resolves registered requests; unknown jobs never reach its subscribers.
/// </remarks>
internal sealed class ObservedJobState : JobState
{
    /// <summary>
    /// Gets or sets the original stored document.
    /// </summary>
    [BsonIgnore]
    public BsonDocument Document { get; set; } = new();
}
