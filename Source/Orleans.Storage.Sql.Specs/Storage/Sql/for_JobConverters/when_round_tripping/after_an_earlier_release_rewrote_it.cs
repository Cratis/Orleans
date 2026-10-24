// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Cratis.Orleans.Storage.Sql.Jobs;

using SqlJob = Cratis.Orleans.Storage.Sql.Jobs.Job;

namespace Cratis.Orleans.Storage.Sql.for_JobConverters.when_round_tripping;

public class after_an_earlier_release_rewrote_it : for_JobStorage.given.jobs_in_storage
{
    JobStepId _stopped;
    SqlJob _entity;
    JobState _result;

    void Establish()
    {
        _stopped = JobStepId.New();
        _firstJob.CompletedStepsCounted = [JobStepId.New()];
        _firstJob.StoppedStepsCounted = [_stopped];
        _firstJob.Progress.SuccessfulSteps = 1;
        _entity = ToEntity(ToEntity(_firstJob).ToJobState(_serializerOptions));

        // Mixed clusters have reduced protection: an older writer drops this record, which cannot be reconstructed.
        var state = JsonNode.Parse(_entity.StateJson)!.AsObject();
        state.Remove("completedStepsCounted");
        _entity.StateJson = state.ToJsonString();
    }

    void Because() => _result = _entity.ToJobState(_serializerOptions);

    [Fact] void should_not_reconstruct_outcomes_dropped_by_the_older_silo() => _result.CompletedStepsCounted.ShouldBeEmpty();
    [Fact] void should_preserve_counted_stops() => _result.StoppedStepsCounted.ShouldContainOnly(_stopped);
    [Fact] void should_preserve_progress() => _result.Progress.SuccessfulSteps.ShouldEqual(1);
}
