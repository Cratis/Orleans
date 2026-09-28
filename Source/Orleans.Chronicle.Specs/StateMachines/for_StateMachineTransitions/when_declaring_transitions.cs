// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines.Orders;

namespace Cratis.Orleans.Chronicle.StateMachines.for_StateMachineTransitions;

public class when_declaring_transitions : Specification
{
    StateMachineTransitions<Order> _transitions;

    void Establish() => _transitions = new();

    void Because() => _transitions
        .On<OrderApproved>().When(order => order.Lines.Any()).TransitionTo<Approved>()
        .On<IOrderClosed>().TransitionTo<Closed>();

    [Fact] void should_have_both_transitions_in_declaration_order() => _transitions.Transitions.Select(_ => _.TargetState).ShouldContainOnly(typeof(Approved), typeof(Closed));
    [Fact] void should_have_the_first_transition_for_its_event() => _transitions.Transitions[0].EventType.ShouldEqual(typeof(OrderApproved));
    [Fact] void should_guard_the_first_transition() => _transitions.Transitions[0].IsGuarded.ShouldBeTrue();
    [Fact] void should_not_guard_the_second_transition() => _transitions.Transitions[1].IsGuarded.ShouldBeFalse();
    [Fact] void should_expose_the_declared_event_types() => _transitions.EventTypes.ShouldContainOnly(typeof(OrderApproved), typeof(IOrderClosed));
}
