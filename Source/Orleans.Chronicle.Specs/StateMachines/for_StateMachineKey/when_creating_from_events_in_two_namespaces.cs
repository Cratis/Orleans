// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines.Orders;

namespace Cratis.Orleans.Chronicle.StateMachines.for_StateMachineKey;

public class when_creating_from_events_in_two_namespaces : Specification
{
    StateMachineKey _first;
    StateMachineKey _second;

    void Because()
    {
        _first = StateMachineKey.From(EventContexts.For<OrderPlaced>("tenant-a", "order-1"));
        _second = StateMachineKey.From(EventContexts.For<OrderPlaced>("tenant-b", "order-1"));
    }

    [Fact] void should_give_different_keys() => _first.ToString().ShouldNotEqual(_second.ToString());
    [Fact] void should_keep_the_namespace_of_the_first_event() => _first.Namespace.Value.ShouldEqual("tenant-a");
    [Fact] void should_keep_the_namespace_of_the_second_event() => _second.Namespace.Value.ShouldEqual("tenant-b");
    [Fact] void should_keep_the_event_store_of_the_event() => _first.EventStore.Value.ShouldEqual("some-store");
    [Fact] void should_keep_the_event_source_id_of_the_event() => _first.EventSourceId.Value.ShouldEqual("order-1");
}
