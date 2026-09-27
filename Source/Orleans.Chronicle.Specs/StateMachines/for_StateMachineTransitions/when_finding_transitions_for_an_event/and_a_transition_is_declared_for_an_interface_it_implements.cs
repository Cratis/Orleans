// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines.Orders;

namespace Cratis.Orleans.Chronicle.StateMachines.for_StateMachineTransitions.when_finding_transitions_for_an_event;

public class and_a_transition_is_declared_for_an_interface_it_implements : Specification
{
    StateMachineTransitions<Order> _transitions;
    IEnumerable<StateMachineTransition<Order>> _result;

    void Establish()
    {
        _transitions = new();
        _transitions
            .On<OrderApproved>().TransitionTo<Approved>()
            .On<IOrderClosed>().TransitionTo<Closed>();
    }

    void Because() => _result = _transitions.For(typeof(OrderShipped));

    [Fact] void should_find_only_the_interface_transition() => _result.Select(_ => _.TargetState).ShouldContainOnly(typeof(Closed));
}
