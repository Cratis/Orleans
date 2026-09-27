// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Projections;
using Cratis.Orleans.StateMachines;

namespace Cratis.Orleans.Chronicle.StateMachines.Tickets;

public class PassiveTicketLifecycle : EventSourcedStateMachine<PassiveTicket>
{
    public override IImmutableList<IState<PassiveTicket>> CreateStates() => [];

    protected override void DefineProjection(IProjectionBuilderFor<PassiveTicket> projection) => projection
        .Passive()
        .From<TicketOpened>()
        .RemovedWith<TicketResolved>();
}
