// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStateSerializer.when_round_tripping;

public class without_a_step_plan : for_JobStorage.given.a_job_storage
{
    JobState _result;

    void Because()
    {
        var document = _firstJob.ToBsonDocument();
        document.Remove("stepPlan");
        _result = BsonSerializer.Deserialize<JobState>(document);
    }

    [Fact] void should_load_old_state_without_inventing_a_plan() => _result.StepPlan.ShouldBeNull();
    [Fact] void should_preserve_the_job() => _result.Id.ShouldEqual(_firstJob.Id);
}
