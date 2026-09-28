// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines.Tickets;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachineInspector.when_inspecting;

public class a_state_machine_defining_its_projection : given.client_artifacts
{
    EventSourcedStateMachineDefinition _definition;

    void Because() => _definition = EventSourcedStateMachineInspector.Inspect(typeof(TicketLifecycle), _artifacts);

    [Fact] void should_have_the_model_type() => _definition.ModelType.ShouldEqual(typeof(Ticket));
    [Fact] void should_have_the_projection_defined_by_the_state_machine() => _definition.ProjectionSource.ShouldEqual(StateMachineProjectionSource.DefinedByStateMachine);
    [Fact] void should_not_be_passive() => _definition.IsPassive.ShouldBeFalse();
    [Fact] void should_have_the_events_the_projection_consumes() => _definition.ProjectionEventTypes.ShouldContainOnly(typeof(TicketOpened), typeof(TicketAssigned), typeof(TicketResolved));
    [Fact] void should_have_the_transition_events() => _definition.TransitionEventTypes.ShouldContainOnly(typeof(TicketAssigned), typeof(TicketResolved), typeof(TicketReopened));
    [Fact] void should_be_interested_in_the_union_of_both() => _definition.EventTypes.ShouldContainOnly(typeof(TicketOpened), typeof(TicketAssigned), typeof(TicketResolved), typeof(TicketReopened));
    [Fact] void should_register_the_projection() => _definition.RegisterProjection.ShouldNotBeNull();
}
