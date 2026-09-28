// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines.Tickets;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachineInspector.when_inspecting;

public class a_state_machine_defining_a_passive_projection : given.client_artifacts
{
    EventSourcedStateMachineDefinition _definition;

    void Because() => _definition = EventSourcedStateMachineInspector.Inspect(typeof(PassiveTicketLifecycle), _artifacts);

    [Fact] void should_be_passive() => _definition.IsPassive.ShouldBeTrue();
    [Fact] void should_have_the_events_the_projection_consumes_including_removal() => _definition.ProjectionEventTypes.ShouldContainOnly(typeof(TicketOpened), typeof(TicketResolved));
    [Fact] void should_have_no_transition_events() => _definition.TransitionEventTypes.ShouldBeEmpty();
}
