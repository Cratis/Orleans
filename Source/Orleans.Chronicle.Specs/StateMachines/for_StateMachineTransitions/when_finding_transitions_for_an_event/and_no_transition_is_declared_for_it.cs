// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines.Orders;

namespace Cratis.Orleans.Chronicle.StateMachines.for_StateMachineTransitions.when_finding_transitions_for_an_event;

public class and_no_transition_is_declared_for_it : Specification
{
    StateMachineTransitions<Order> _transitions;
    IEnumerable<StateMachineTransition<Order>> _result;

    void Establish()
    {
        _transitions = new();
        _transitions.On<OrderApproved>().TransitionTo<Approved>();
    }

    void Because() => _result = _transitions.For(typeof(LineAdded));

    [Fact] void should_find_none() => _result.ShouldBeEmpty();
}
