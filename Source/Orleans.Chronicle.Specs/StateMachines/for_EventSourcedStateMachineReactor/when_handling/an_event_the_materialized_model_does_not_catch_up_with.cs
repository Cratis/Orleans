// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Orleans.Chronicle.StateMachines.Tickets;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachineReactor.when_handling;

public class an_event_the_materialized_model_does_not_catch_up_with : given.a_reactor
{
    EventContext _context;
    Exception _error;

    protected override Type StateMachineType => typeof(TicketLifecycle);

    void Establish()
    {
        _options.ModelCatchUpTimeout = TimeSpan.Zero;
        _context = EventContexts.For<TicketResolved>("tenant-a", "ticket-1", sequenceNumber: 42);
        _eventStores.For("tenant-a").Projections.GetStateForModel(_definition.ModelType).Returns(ProjectionAt(41));
    }

    async Task Because() => _error = await Catch.Exception(() => Reactor.Handle(new TicketResolved(), _context));

    [Fact] void should_fail_the_delivery() => _error.ShouldBeOfExactType<ModelDidNotCatchUp>();
    [Fact] void should_not_refresh_the_state_machine_with_a_stale_model() => _grainsByKey.Values.ShouldBeEmpty();
}
