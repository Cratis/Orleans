// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines.Shipments;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachineInspector.when_inspecting;

public class a_state_machine_whose_projection_chronicle_discovered : given.client_artifacts
{
    EventSourcedStateMachineDefinition _definition;

    void Establish() => _discoveredProjections.Add(typeof(ShipmentProjection));

    void Because() => _definition = EventSourcedStateMachineInspector.Inspect(typeof(ShipmentTracking), _artifacts);

    [Fact] void should_say_the_projection_is_already_discovered() => _definition.ProjectionSource.ShouldEqual(StateMachineProjectionSource.AlreadyDiscovered);
    [Fact] void should_not_register_the_projection() => _definition.RegisterProjection.ShouldBeNull();
    [Fact] void should_have_the_events_the_discovered_projection_consumes() => _definition.ProjectionEventTypes.ShouldContainOnly(typeof(ShipmentDispatched), typeof(ShipmentDelivered));
}
