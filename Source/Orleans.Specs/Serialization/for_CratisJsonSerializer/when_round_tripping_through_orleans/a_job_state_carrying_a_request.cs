// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Serialization.for_CratisJsonSerializer.when_round_tripping_through_orleans;

/// <summary>
/// The cross-silo path for the same shape the deep-copy specs cover: a serialized job state must
/// come back with its interface-typed request as the concrete type, regardless of what the host
/// did to the shared JSON codec's options.
/// </summary>
public class a_job_state_carrying_a_request : given.a_serializer_with_a_host_json_serializer
{
    JobId _jobId;
    JobState _original;
    JobState _result;

    void Establish()
    {
        _jobId = JobId.New();
        _original = new JobState
        {
            Id = _jobId,
            Type = typeof(NullJobWithSomeRequest),
            Details = "some details",
            Status = JobStatus.CompletedSuccessfully,
            Created = new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero),
            Request = new SomeJobRequest(42)
        };
    }

    void Because() => _result = RoundTrip(_original);

    [Fact] void should_preserve_the_id() => _result.Id.ShouldEqual(_jobId);
    [Fact] void should_preserve_the_status() => _result.Status.ShouldEqual(JobStatus.CompletedSuccessfully);
    [Fact] void should_carry_the_request_as_its_concrete_type() => _result.Request.ShouldBeOfExactType<SomeJobRequest>();
    [Fact] void should_preserve_the_request_content() => ((SomeJobRequest)_result.Request).SomeNumber.ShouldEqual(42);
}
