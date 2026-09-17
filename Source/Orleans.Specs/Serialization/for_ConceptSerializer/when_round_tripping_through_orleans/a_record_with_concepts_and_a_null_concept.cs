// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Orleans.Serialization.for_ConceptSerializer.when_round_tripping_through_orleans;

public class a_record_with_concepts_and_a_null_concept : given.a_configured_orleans_serializer
{
    Envelope _original;
    Envelope _result;

    void Establish() => _original = new Envelope(JobId.New(), "the-source", null);

    void Because() => _result = RoundTrip(_original);

    [Fact] void should_round_trip_the_guid_concept() => _result.JobId.ShouldEqual(_original.JobId);
    [Fact] void should_round_trip_the_string_concept() => _result.Source.ShouldEqual(_original.Source);
    [Fact] void should_round_trip_the_null_concept_as_null() => _result.Optional.ShouldBeNull();

    [GenerateSerializer]
    public record Envelope(
        [Id(0)] JobId JobId,
        [Id(1)] string Source,
        [Id(2)] JobName? Optional);
}
