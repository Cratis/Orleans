// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStateSerializer.when_round_tripping;

public class without_a_completed_steps_counted_element : for_JobStorage.given.a_job_storage
{
    BsonDocument _document;
    JobState _result;

    void Establish()
    {
        _document = _firstJob.ToBsonDocument();
        _document.Remove("completedStepsCounted");
    }

    void Because() => _result = BsonSerializer.Deserialize<JobState>(_document);

    [Fact] void should_read_no_counted_steps() => _result.CompletedStepsCounted.ShouldBeEmpty();
    [Fact] void should_read_the_status() => _result.Status.ShouldEqual(_firstJob.Status);
    [Fact] void should_read_the_request() => _result.Request.ShouldEqual(_firstJob.Request);
}
