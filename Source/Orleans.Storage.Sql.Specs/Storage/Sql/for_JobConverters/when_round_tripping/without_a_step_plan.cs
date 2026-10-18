// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Orleans.Storage.Jobs;
using Cratis.Orleans.Storage.Sql.Jobs;

namespace Cratis.Orleans.Storage.Sql.for_JobConverters.when_round_tripping;

public class without_a_step_plan : for_JobStorage.given.jobs_in_storage
{
    JobState _result;

    void Because()
    {
        var entity = ToEntity(_firstJob);
        var node = JsonNode.Parse(entity.StateJson)!.AsObject();
        node.Remove("stepPlan");
        entity.StateJson = node.ToJsonString();
        _result = entity.ToJobState(_serializerOptions);
    }

    [Fact] void should_load_old_state_without_inventing_a_plan() => _result.StepPlan.ShouldBeNull();
    [Fact] void should_preserve_the_job() => _result.Id.ShouldEqual(_firstJob.Id);
}
