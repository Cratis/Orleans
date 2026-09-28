// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Orleans.Chronicle.StateMachines.Orders;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachineReactor.when_handling;

public class an_event_for_a_passive_model : given.a_reactor
{
    EventContext _context;
    StateMachineRefresh _refresh;

    protected override Type StateMachineType => typeof(OrderProcess);

    void Establish()
    {
        _context = EventContexts.For<OrderApproved>("tenant-a", "order-1");
        GrainFor(StateMachineKey.From(_context)).When(_ => _.Refresh(Arg.Any<StateMachineRefresh>())).Do(call => _refresh = call.Arg<StateMachineRefresh>());
    }

    Task Because() => Reactor.Handle(new OrderApproved(), _context);

    [Fact] void should_resolve_the_grain_of_the_state_machine_for_the_event_source() => _grains.Received(1).Get(typeof(OrderProcess), new StateMachineKey("some-store", "tenant-a", "order-1"));
    [Fact] void should_refresh_it_for_the_event_type() => _refresh.EventType.ShouldEqual(typeof(OrderApproved));
    [Fact] void should_refresh_it_with_the_event_context() => _refresh.GetContext().SequenceNumber.ShouldEqual(_context.SequenceNumber);
    [Fact] void should_not_wait_for_the_model() => _eventStores.Provider.ReceivedCalls().ShouldBeEmpty();
}
