// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines.Orders;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachineInspector.when_inspecting;

public class a_type_that_is_not_a_state_machine : given.client_artifacts
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => EventSourcedStateMachineInspector.Inspect(typeof(Order), _artifacts));

    [Fact] void should_throw_type_is_not_an_event_sourced_state_machine() => _error.ShouldBeOfExactType<TypeIsNotAnEventSourcedStateMachine>();
}
