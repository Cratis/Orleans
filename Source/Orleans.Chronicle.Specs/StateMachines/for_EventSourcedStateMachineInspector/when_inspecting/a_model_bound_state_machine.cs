// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines.Orders;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachineInspector.when_inspecting;

public class a_model_bound_state_machine : given.client_artifacts
{
    EventSourcedStateMachineDefinition _definition;

    void Because() => _definition = EventSourcedStateMachineInspector.Inspect(typeof(OrderProcess), _artifacts);

    [Fact] void should_have_the_model_type() => _definition.ModelType.ShouldEqual(typeof(Order));
    [Fact] void should_have_the_model_bound_projection_registered_by_the_integration() => _definition.ProjectionSource.ShouldEqual(StateMachineProjectionSource.ModelBound);
    [Fact] void should_be_passive_as_the_model_says() => _definition.IsPassive.ShouldBeTrue();
    [Fact] void should_have_the_events_the_projection_consumes_including_children() => _definition.ProjectionEventTypes.ShouldContainOnly(typeof(OrderPlaced), typeof(OrderApproved), typeof(OrderShipped), typeof(OrderCancelled), typeof(LineAdded));
    [Fact] void should_have_the_transition_events_with_the_interface_expanded() => _definition.TransitionEventTypes.ShouldContainOnly(typeof(OrderApproved), typeof(OrderShipped), typeof(OrderCancelled), typeof(OrderEscalated));
    [Fact] void should_be_interested_in_the_union_of_both() => _definition.EventTypes.ShouldContainOnly(typeof(OrderPlaced), typeof(OrderApproved), typeof(OrderShipped), typeof(OrderCancelled), typeof(LineAdded), typeof(OrderEscalated));
    [Fact] void should_have_a_reactor_identified_by_the_state_machine() => _definition.ReactorId.Value.ShouldEqual(typeof(OrderProcess).FullName);
    [Fact] void should_register_the_projection() => _definition.RegisterProjection.ShouldNotBeNull();
}
