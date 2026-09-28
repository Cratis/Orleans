// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines.Orders;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachineInspector.when_inspecting;

public class a_model_bound_state_machine_whose_model_chronicle_discovered : given.client_artifacts
{
    EventSourcedStateMachineDefinition _definition;

    void Establish() => _discoveredModelBoundProjections.Add(typeof(Order));

    void Because() => _definition = EventSourcedStateMachineInspector.Inspect(typeof(OrderProcess), _artifacts);

    [Fact] void should_say_the_projection_is_already_discovered() => _definition.ProjectionSource.ShouldEqual(StateMachineProjectionSource.AlreadyDiscovered);
    [Fact] void should_not_register_the_projection_again() => _definition.RegisterProjection.ShouldBeNull();
    [Fact] void should_still_know_the_events_the_projection_consumes() => _definition.ProjectionEventTypes.ShouldContain(typeof(OrderPlaced));
}
