// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines.Misfits;
using Cratis.Orleans.Chronicle.StateMachines.Orders;

namespace Cratis.Orleans.Chronicle.StateMachines.for_StateMachineTransitions;

public class when_declaring_a_transition_for_a_type_that_is_not_an_event : Specification
{
    StateMachineTransitions<Order> _transitions;
    Exception _error;

    void Establish() => _transitions = new();

    void Because() => _error = Catch.Exception(() => _transitions.On<NotAnEvent>());

    [Fact] void should_throw_transition_event_is_not_an_event_type() => _error.ShouldBeOfExactType<TransitionEventIsNotAnEventType>();
}
