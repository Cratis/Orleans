// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Serialization.for_CratisJsonSerializer.when_deep_copying;

/// <summary>
/// The exact shape production failed on: <c language="csharp">IJobsManager.GetJobsOfType</c> answers an
/// <c language="csharp">ImmutableList&lt;JobState&gt;</c> whose <c language="csharp">Request</c> is interface-typed, and
/// Orleans deep-copies the response on every same-silo grain call.
/// </summary>
public class a_list_of_job_states_carrying_requests : given.a_serializer_with_a_host_json_serializer
{
    JobId _jobId;
    ImmutableList<JobState> _original;
    ImmutableList<JobState> _result;

    void Establish()
    {
        _jobId = JobId.New();
        _original =
        [
            new JobState
            {
                Id = _jobId,
                Type = typeof(NullJobWithSomeRequest),
                Details = "some details",
                Status = JobStatus.Running,
                Created = new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero),
                Request = new SomeJobRequest(42)
            }
        ];
    }

    void Because() => _result = _copier.Copy(_original);

    [Fact] void should_produce_a_distinct_instance() => _result[0].ShouldNotBeSame(_original[0]);
    [Fact] void should_preserve_the_id() => _result[0].Id.ShouldEqual(_jobId);
    [Fact] void should_preserve_the_status() => _result[0].Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_preserve_the_created_time() => _result[0].Created.ShouldEqual(_original[0].Created);
    [Fact] void should_carry_the_request_as_its_concrete_type() => _result[0].Request.ShouldBeOfExactType<SomeJobRequest>();
    [Fact] void should_preserve_the_request_content() => ((SomeJobRequest)_result[0].Request).SomeNumber.ShouldEqual(42);
}
