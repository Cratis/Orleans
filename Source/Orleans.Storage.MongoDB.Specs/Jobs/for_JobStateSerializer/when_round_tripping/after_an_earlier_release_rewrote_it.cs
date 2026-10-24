// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStateSerializer.when_round_tripping;

public class after_an_earlier_release_rewrote_it : for_JobStorage.given.a_job_storage
{
    JobStepId _stopped;
    BsonDocument _document;
    JobState _result;

    void Establish()
    {
        _stopped = JobStepId.New();
        _firstJob.CompletedStepsCounted = [JobStepId.New()];
        _firstJob.StoppedStepsCounted = [_stopped];
        _firstJob.Progress.SuccessfulSteps = 1;
        _document = BsonSerializer.Deserialize<JobState>(_firstJob.ToBsonDocument()).ToBsonDocument();

        // Mixed clusters have reduced protection: an older writer drops this record, which cannot be reconstructed.
        _document.Remove("completedStepsCounted");
    }

    void Because() => _result = BsonSerializer.Deserialize<JobState>(_document);

    [Fact] void should_not_reconstruct_outcomes_dropped_by_the_older_silo() => _result.CompletedStepsCounted.ShouldBeEmpty();
    [Fact] void should_preserve_counted_stops() => _result.StoppedStepsCounted.ShouldContainOnly(_stopped);
    [Fact] void should_preserve_progress() => _result.Progress.SuccessfulSteps.ShouldEqual(1);
}
