// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Orleans.Chronicle.StateMachines.Orders;

namespace Cratis.Orleans.Chronicle.StateMachines.for_StateMachineRefresh;

public class when_getting_the_context_of_a_delivered_event : Specification
{
    EventContext _context;
    StateMachineRefresh _refresh;
    EventContext _result;

    void Establish()
    {
        _context = EventContexts.For<OrderApproved>("tenant-a", "order-1", sequenceNumber: 7);
        _refresh = StateMachineRefresh.For(typeof(OrderApproved), _context);
    }

    void Because() => _result = _refresh.GetContext();

    [Fact] void should_keep_the_event_type() => _result.EventType.ShouldEqual(_context.EventType);
    [Fact] void should_keep_the_event_source_id() => _result.EventSourceId.ShouldEqual(_context.EventSourceId);
    [Fact] void should_keep_the_sequence_number() => _result.SequenceNumber.ShouldEqual(_context.SequenceNumber);
    [Fact] void should_keep_the_event_store() => _result.EventStore.ShouldEqual(_context.EventStore);
    [Fact] void should_keep_the_namespace() => _result.Namespace.ShouldEqual(_context.Namespace);
    [Fact] void should_keep_the_correlation_id() => _result.CorrelationId.ShouldEqual(_context.CorrelationId);
    [Fact] void should_keep_when_it_occurred() => _result.Occurred.ShouldEqual(_context.Occurred);
    [Fact] void should_keep_the_clr_event_type() => _refresh.EventType.ShouldEqual(typeof(OrderApproved));
}
