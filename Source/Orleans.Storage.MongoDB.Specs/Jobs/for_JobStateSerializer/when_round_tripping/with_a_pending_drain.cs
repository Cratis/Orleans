// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStateSerializer.when_round_tripping;

public class with_a_pending_drain : for_JobStorage.given.a_job_storage
{
    BsonDocument _document;
    JobState _result;

    void Establish() => _firstJob.DrainPending = true;

    void Because()
    {
        _document = _firstJob.ToBsonDocument();
        _result = BsonSerializer.Deserialize<JobState>(_document);
    }

    [Fact] void should_write_the_pending_drain() => _document["drainPending"].AsBoolean.ShouldBeTrue();
    [Fact] void should_read_the_pending_drain() => _result.DrainPending.ShouldBeTrue();
}
