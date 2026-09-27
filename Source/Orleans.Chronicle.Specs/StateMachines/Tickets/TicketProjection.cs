// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Orleans.Chronicle.StateMachines.Tickets;

public class TicketProjection : IProjectionFor<Ticket>
{
    public void Define(IProjectionBuilderFor<Ticket> builder) => builder.From<TicketOpened>();
}
