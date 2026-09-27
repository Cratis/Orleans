// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Orleans.Chronicle.StateMachines.Tickets;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachineReactor.when_handling;

public class an_event_the_materialized_model_catches_up_with_while_waiting : given.a_reactor
{
    EventContext _context;
    List<string> _order;

    protected override Type StateMachineType => typeof(TicketLifecycle);

    void Establish()
    {
        _order = [];
        _context = EventContexts.For<TicketResolved>("tenant-a", "ticket-1", sequenceNumber: 42);
        _eventStores.For("tenant-a").Projections.GetStateForModel(_definition.ModelType).Returns(
            _ =>
            {
                _order.Add("checked");
                return ProjectionAt(40);
            },
            _ =>
            {
                _order.Add("checked");
                return ProjectionAt(42);
            });
        GrainFor(StateMachineKey.From(_context)).When(_ => _.Refresh(Arg.Any<StateMachineRefresh>())).Do(_ => _order.Add("refreshed"));
    }

    Task Because() => Reactor.Handle(new TicketResolved(), _context);

    [Fact] void should_only_refresh_once_the_model_has_caught_up() => _order.ShouldContainOnly("checked", "checked", "refreshed");
    [Fact] void should_refresh_after_checking() => _order[^1].ShouldEqual("refreshed");
}
