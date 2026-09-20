// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Serialization.for_CratisJsonSerializer.when_checking_if_type_is_supported;

public class with_various_types : Specification
{
    CratisJsonSerializer _serializer;

    void Establish() => _serializer = new CratisJsonSerializer();

    [Fact] void should_support_a_job_state() => _serializer.IsSupportedType(typeof(JobState)).ShouldBeTrue();
    [Fact] void should_support_a_cratis_record() => _serializer.IsSupportedType(typeof(JobStepDetails)).ShouldBeTrue();
    [Fact] void should_not_support_a_one_of_derivative() => _serializer.IsSupportedType(typeof(Result<JobId, StartJobError>)).ShouldBeFalse();
    [Fact] void should_not_support_an_interface() => _serializer.IsSupportedType(typeof(IJobRequest)).ShouldBeFalse();
    [Fact] void should_not_support_a_host_type() => _serializer.IsSupportedType(typeof(SomeHost.SomeHostRecord)).ShouldBeFalse();
}
