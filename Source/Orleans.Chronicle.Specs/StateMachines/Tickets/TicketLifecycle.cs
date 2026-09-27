// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Projections;
using Cratis.Orleans.StateMachines;

namespace Cratis.Orleans.Chronicle.StateMachines.Tickets;

public class TicketLifecycle : EventSourcedStateMachine<Ticket>
{
    protected override Type InitialState => typeof(Open);

    public override IImmutableList<IState<Ticket>> CreateStates() => [new Open(), new InProgress(), new Done()];

    protected override void DefineProjection(IProjectionBuilderFor<Ticket> projection) => projection
        .From<TicketOpened>()
        .From<TicketAssigned>()
        .From<TicketResolved>(_ => _.Set(ticket => ticket.Resolved).ToValue(true));

    protected override void DefineTransitions(IStateMachineTransitions<Ticket> transitions) => transitions
        .On<TicketAssigned>().TransitionTo<InProgress>()
        .On<TicketResolved>().TransitionTo<Done>()
        .On<TicketReopened>().TransitionTo<Open>();

    protected override Type ResolveState(Ticket? model) => model switch
    {
        { Resolved: true } => typeof(Done),
        { Assignee.Length: > 0 } => typeof(InProgress),
        _ => InitialState
    };
}
