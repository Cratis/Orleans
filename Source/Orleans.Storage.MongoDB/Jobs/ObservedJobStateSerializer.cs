// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;
using Cratis.Orleans.Storage.MongoDB.Serialization;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.Orleans.Storage.MongoDB.Jobs;

/// <summary>
/// Reads observation metadata without deserializing job requests and preserves the original document.
/// </summary>
[BsonSerializerDisableAutoRegistration]
internal sealed class ObservedJobStateSerializer : SerializerBase<ObservedJobState>
{
    /// <inheritdoc/>
    public override ObservedJobState Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var document = BsonDocumentSerializer.Instance.Deserialize(context);
        var metadata = document.DeepClone().AsBsonDocument;
        metadata.Remove("request");
        var state = BsonSerializer.Deserialize<JobState>(metadata);
        return new ObservedJobState
        {
            Id = state.Id,
            Details = state.Details,
            Type = state.Type,
            Status = state.Status,
            Created = state.Created,
            StatusChanges = state.StatusChanges,
            Progress = state.Progress,
            Document = document
        };
    }

    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ObservedJobState value) =>
        BsonDocumentSerializer.Instance.Serialize(context, value.Document);
}
