// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Orleans.Chronicle.StateMachines.Tickets;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachineReactor.when_handling;

public class a_transition_only_event_for_a_materialized_model : given.a_reactor
{
    EventContext _context;

    protected override Type StateMachineType => typeof(TicketLifecycle);

    void Establish() => _context = EventContexts.For<TicketReopened>("tenant-a", "ticket-1");

    Task Because() => Reactor.Handle(new TicketReopened(), _context);

    [Fact] void should_not_wait_for_the_projection_which_never_handles_the_event() => _eventStores.Provider.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_refresh_the_state_machine() => GrainFor(StateMachineKey.From(_context)).Received(1).Refresh(Arg.Any<StateMachineRefresh>());
}
