// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Orleans.Storage.MongoDB.Jobs.for_JobStepState.when_deserializing;

public class a_document_written_before_attempts_and_leases_existed : Jobs.given.a_database
{
    JobStepState _state;

    void Establish()
    {
        var document = new JobStepState { Id = new(JobId.New(), JobStepId.New()), Type = typeof(IJobStep), Status = JobStepStatus.Running }.ToBsonDocument();
        document.Remove("attempt");
        document.Remove("lease");
        _state = BsonSerializer.Deserialize<JobStepState>(document);
    }

    [Fact] void should_read_it_as_the_first_attempt() => _state.Attempt.ShouldEqual(1);
    [Fact] void should_read_it_without_a_lease() => _state.Lease.ShouldBeNull();
}
