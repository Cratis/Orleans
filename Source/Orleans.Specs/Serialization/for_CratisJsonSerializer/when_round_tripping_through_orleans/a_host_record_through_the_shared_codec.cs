// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SomeHost;

namespace Cratis.Orleans.Serialization.for_CratisJsonSerializer.when_round_tripping_through_orleans;

/// <summary>
/// The host's own arrangement keeps working: its types still ride Orleans' shared JSON codec with
/// the options the host registered.
/// </summary>
public class a_host_record_through_the_shared_codec : given.a_serializer_with_a_host_json_serializer
{
    SomeHostRecord _original;
    SomeHostRecord _result;

    void Establish() => _original = new SomeHostRecord("some content");

    void Because() => _result = RoundTrip(_original);

    [Fact] void should_round_trip_to_the_same_value() => _result.ShouldEqual(_original);
}
