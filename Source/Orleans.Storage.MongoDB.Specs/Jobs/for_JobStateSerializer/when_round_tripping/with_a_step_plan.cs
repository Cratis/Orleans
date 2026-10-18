// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStateSerializer.when_round_tripping;

public class with_a_step_plan : for_JobStorage.given.a_job_storage
{
    PlannedJobStep _planned;
    JobState _result;

    void Establish()
    {
        _planned = new(JobStepId.New(), typeof(IJob), 10, typeof(KnownRequest).AssemblyQualifiedName!, "{\"message\":\"first\"}");
        _firstJob.StepPlan = [_planned];
    }

    void Because() => _result = BsonSerializer.Deserialize<JobState>(_firstJob.ToBsonDocument());

    [Fact] void should_preserve_the_identity_stage_type_and_request() => _result.StepPlan!.Single().ShouldEqual(_planned);
}
