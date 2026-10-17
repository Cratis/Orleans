// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStepState.when_deserializing;

public class a_document_with_an_attempt_and_a_lease : Jobs.given.a_database
{
    JobStepState _state;

    void Establish()
    {
        var document = new JobStepState
        {
            Id = new(JobId.New(), JobStepId.New()),
            Type = typeof(IJobStep),
            Status = JobStepStatus.Running,
            Attempt = 4,
            Lease = new() { Owner = "silo-1", LastHeartbeat = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), Attempt = 4 }
        }.ToBsonDocument();
        _state = BsonSerializer.Deserialize<JobStepState>(document);
    }

    [Fact] void should_read_the_attempt() => _state.Attempt.ShouldEqual(4);
    [Fact] void should_read_the_lease_owner() => _state.Lease!.Owner.ShouldEqual("silo-1");
}
