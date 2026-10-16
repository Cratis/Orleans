// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStateSerializer.when_round_tripping;

public class with_an_element_this_release_does_not_know : for_JobStorage.given.a_job_storage
{
    BsonDocument _document;
    JobState _result;

    void Establish()
    {
        // How a release without drains reads a document written by one with them.
        _document = _firstJob.ToBsonDocument();
        _document["elementOfALaterRelease"] = true;
    }

    void Because() => _result = BsonSerializer.Deserialize<JobState>(_document);

    [Fact] void should_read_the_status() => _result.Status.ShouldEqual(_firstJob.Status);
    [Fact] void should_read_the_request() => _result.Request.ShouldEqual(_firstJob.Request);
}
