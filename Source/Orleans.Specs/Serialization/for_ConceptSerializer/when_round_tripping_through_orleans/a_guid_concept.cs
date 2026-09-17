// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Orleans.Serialization.for_ConceptSerializer.when_round_tripping_through_orleans;

public class a_guid_concept : given.a_configured_orleans_serializer
{
    JobId _original;
    JobId _result;

    void Establish() => _original = JobId.New();

    void Because() => _result = RoundTrip(_original);

    [Fact] void should_round_trip_to_the_same_value() => _result.ShouldEqual(_original);
}
