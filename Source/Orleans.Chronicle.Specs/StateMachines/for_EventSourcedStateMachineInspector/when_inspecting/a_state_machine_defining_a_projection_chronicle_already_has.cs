// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines.Tickets;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachineInspector.when_inspecting;

public class a_state_machine_defining_a_projection_chronicle_already_has : given.client_artifacts
{
    Exception _error;

    void Establish() => _discoveredProjections.Add(typeof(TicketProjection));

    void Because() => _error = Catch.Exception(() => EventSourcedStateMachineInspector.Inspect(typeof(TicketLifecycle), _artifacts));

    [Fact] void should_throw_state_machine_model_already_has_projection() => _error.ShouldBeOfExactType<StateMachineModelAlreadyHasProjection>();
}
