// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Chronicle.StateMachines.for_StateMachineKey.when_parsing;

public class and_the_event_source_id_contains_the_separator : Specification
{
    StateMachineKey _result;

    void Because() => _result = StateMachineKey.Parse("some-store/tenant-a/orders/1");

    [Fact] void should_take_the_event_store_from_the_first_part() => _result.EventStore.Value.ShouldEqual("some-store");
    [Fact] void should_take_the_namespace_from_the_second_part() => _result.Namespace.Value.ShouldEqual("tenant-a");
    [Fact] void should_keep_the_rest_as_the_event_source_id() => _result.EventSourceId.Value.ShouldEqual("orders/1");
}
