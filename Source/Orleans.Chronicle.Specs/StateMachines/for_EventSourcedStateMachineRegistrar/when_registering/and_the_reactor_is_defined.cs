// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Orleans.Chronicle.StateMachines.Orders;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachineRegistrar.when_registering;

public class and_the_reactor_is_defined : given.registrations
{
    IReactorBuilder _builder;

    void Establish() => _builder = Substitute.For<IReactorBuilder>();

    void Because()
    {
        Register(typeof(OrderProcess));
        _reactors.Values.Single()(_builder);
    }

    [Fact] void should_handle_order_placed_from_the_projection() => _builder.Received(1).On(Arg.Any<Func<OrderPlaced, EventContext, Task>>());
    [Fact] void should_handle_line_added_from_the_projection_children() => _builder.Received(1).On(Arg.Any<Func<LineAdded, EventContext, Task>>());
    [Fact] void should_handle_order_approved_once_although_both_projection_and_transition_use_it() => _builder.Received(1).On(Arg.Any<Func<OrderApproved, EventContext, Task>>());
    [Fact] void should_handle_order_shipped() => _builder.Received(1).On(Arg.Any<Func<OrderShipped, EventContext, Task>>());
    [Fact] void should_handle_order_cancelled() => _builder.Received(1).On(Arg.Any<Func<OrderCancelled, EventContext, Task>>());
    [Fact] void should_handle_order_escalated_from_the_transitions() => _builder.Received(1).On(Arg.Any<Func<OrderEscalated, EventContext, Task>>());
    [Fact] void should_declare_exactly_one_handler_per_event_type() => _builder.ReceivedCalls().Count().ShouldEqual(6);
    [Fact] void should_not_subscribe_to_all_events() => _builder.DidNotReceive().Subscribe(Arg.Any<Func<object, EventContext, Task>>());
}
