// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Orleans.Chronicle.StateMachines.Tickets;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachineReactor.when_handling;

public class an_event_the_materialized_model_has_caught_up_with : given.a_reactor
{
    EventContext _context;

    protected override Type StateMachineType => typeof(TicketLifecycle);

    void Establish()
    {
        _context = EventContexts.For<TicketAssigned>("tenant-a", "ticket-1", sequenceNumber: 42);
        _eventStores.For("tenant-a").Projections.GetStateForModel(_definition.ModelType).Returns(ProjectionAt(42));
    }

    Task Because() => Reactor.Handle(new TicketAssigned("Jane"), _context);

    [Fact] void should_check_the_projection_in_the_namespace_of_the_event() => _eventStores.For("tenant-a").Projections.Received(1).GetStateForModel(_definition.ModelType);
    [Fact] void should_refresh_the_state_machine() => GrainFor(StateMachineKey.From(_context)).Received(1).Refresh(Arg.Any<StateMachineRefresh>());
}
