// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStateSerializer.when_round_tripping;

public class with_counted_stopped_steps : for_JobStorage.given.a_job_storage
{
    JobStepId _first;
    JobStepId _second;
    BsonDocument _document;
    JobState _result;

    void Establish()
    {
        _first = JobStepId.New();
        _second = JobStepId.New();
        _firstJob.StoppedStepsCounted = [_first, _second];
    }

    void Because()
    {
        _document = _firstJob.ToBsonDocument();
        _result = BsonSerializer.Deserialize<JobState>(_document);
    }

    [Fact] void should_write_the_counted_steps() => _document["stoppedStepsCounted"].AsBsonArray.Count.ShouldEqual(2);
    [Fact] void should_read_the_counted_steps() => _result.StoppedStepsCounted.ShouldContainOnly(_first, _second);
}
