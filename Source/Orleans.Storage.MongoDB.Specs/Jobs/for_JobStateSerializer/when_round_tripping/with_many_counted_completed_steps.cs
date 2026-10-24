// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStateSerializer.when_round_tripping;

public class with_many_counted_completed_steps : for_JobStorage.given.a_job_storage
{
    int _baselineBytes;
    int _bytes;
    JobState _result;

    void Establish()
    {
        _baselineBytes = _firstJob.ToBson().Length;
        _firstJob.CompletedStepsCounted = Enumerable.Range(0, 100_000).Select(_ => JobStepId.New()).ToList();
    }

    void Because()
    {
        var document = _firstJob.ToBsonDocument();
        _bytes = document.ToBson().Length;
        _result = BsonSerializer.Deserialize<JobState>(document);
    }

    [Fact] void should_fit_below_four_megabytes() => _bytes.ShouldBeLessThan(4_000_000);
    [Fact] void should_read_every_counted_step() => _result.CompletedStepsCounted.Count.ShouldEqual(100_000);
    [Fact] void should_use_the_measured_bson_bytes_for_entries() => (_bytes - _baselineBytes).ShouldEqual(2_788_890);
}
