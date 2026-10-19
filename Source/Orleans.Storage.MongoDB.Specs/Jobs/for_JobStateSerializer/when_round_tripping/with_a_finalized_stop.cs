// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStateSerializer.when_round_tripping;

public class with_a_finalized_stop : for_JobStorage.given.a_job_storage
{
    BsonDocument _document;
    JobState _result;

    void Establish() => _firstJob.StopFinalized = true;

    void Because()
    {
        _document = _firstJob.ToBsonDocument();
        _result = BsonSerializer.Deserialize<JobState>(_document);
    }

    [Fact] void should_write_the_finalized_stop() => _document["stopFinalized"].AsBoolean.ShouldBeTrue();
    [Fact] void should_read_the_finalized_stop() => _result.StopFinalized.ShouldBeTrue();
}
