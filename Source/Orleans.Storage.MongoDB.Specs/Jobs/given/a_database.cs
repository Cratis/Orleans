// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.given;

public class a_database : Specification
{
    protected IMongoDatabase _database;

    static a_database()
    {
        ConventionPacks.EnsureRegistered();
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        BsonSerializer.RegisterSerializationProvider(new ConceptSerializationProvider());
    }

    void Establish() => _database = Substitute.For<IMongoDatabase>();

    protected IMongoCollection<TDocument> Collection<TDocument>(string name)
    {
        var collection = Substitute.For<IMongoCollection<TDocument>>();
        collection.CollectionNamespace.Returns(new CollectionNamespace("jobs", name));
        collection.Indexes.ListAsync(Arg.Any<ListIndexesOptions>(), Arg.Any<CancellationToken>()).Returns(_ => Cursor(
            [new BsonDocument("name", "status"), new BsonDocument("name", "type_status"), new BsonDocument("name", "jobId")]));
        _database.GetCollection<TDocument>(name).Returns(collection);
        return collection;
    }

    protected static IAsyncCursor<BsonDocument> Cursor(params BsonDocument[][] batches)
    {
        var cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
        var batchIndex = -1;
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(_ => ++batchIndex < batches.Length);
        cursor.Current.Returns(_ => batches[batchIndex]);
        return cursor;
    }
}
