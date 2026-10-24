// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStorage;

public class when_reading_the_indexes : Specification
{
    CreateIndexModel<JobState> _index;

    void Because() => _index = JobStorage.Indexes.Single(index => index.Options.Name == "status_stopFinalized");

    [Fact] void should_index_status_then_stop_finalized() => _index.Keys.Render(new RenderArgs<JobState>(BsonSerializer.LookupSerializer<JobState>(), BsonSerializer.SerializerRegistry)).ShouldEqual(new BsonDocument { { "status", 1 }, { "stopFinalized", 1 } });

    [Fact] void should_build_in_the_background() => _index.Options.Background.ShouldEqual(true);
}
