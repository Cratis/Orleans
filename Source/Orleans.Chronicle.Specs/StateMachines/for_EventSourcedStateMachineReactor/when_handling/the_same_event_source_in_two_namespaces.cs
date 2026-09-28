// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines.Tickets;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachineReactor.when_handling;

public class the_same_event_source_in_two_namespaces : given.a_reactor
{
    protected override Type StateMachineType => typeof(TicketLifecycle);

    void Establish()
    {
        _eventStores.For("tenant-a").Projections.GetStateForModel(_definition.ModelType).Returns(ProjectionAt(42));
        _eventStores.For("tenant-b").Projections.GetStateForModel(_definition.ModelType).Returns(ProjectionAt(42));
    }

    async Task Because()
    {
        await Reactor.Handle(new TicketAssigned("Jane"), EventContexts.For<TicketAssigned>("tenant-a", "ticket-1"));
        await Reactor.Handle(new TicketAssigned("John"), EventContexts.For<TicketAssigned>("tenant-b", "ticket-1"));
    }

    [Fact] void should_refresh_two_distinct_grains() => _grainsByKey.Keys.ShouldContainOnly("some-store/tenant-a/ticket-1", "some-store/tenant-b/ticket-1");
    [Fact] void should_wait_for_the_model_in_the_first_namespace() => _eventStores.For("tenant-a").Projections.Received(1).GetStateForModel(_definition.ModelType);
    [Fact] void should_wait_for_the_model_in_the_second_namespace() => _eventStores.For("tenant-b").Projections.Received(1).GetStateForModel(_definition.ModelType);
}
