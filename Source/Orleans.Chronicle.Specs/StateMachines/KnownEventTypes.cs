// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines.Orders;
using Cratis.Orleans.Chronicle.StateMachines.Shipments;
using Cratis.Orleans.Chronicle.StateMachines.Tickets;

namespace Cratis.Orleans.Chronicle.StateMachines;

public static class KnownEventTypes
{
    public static readonly Type[] All =
    [
        typeof(OrderPlaced),
        typeof(LineAdded),
        typeof(OrderApproved),
        typeof(OrderShipped),
        typeof(OrderCancelled),
        typeof(OrderEscalated),
        typeof(TicketOpened),
        typeof(TicketAssigned),
        typeof(TicketResolved),
        typeof(TicketReopened),
        typeof(ShipmentDispatched),
        typeof(ShipmentDelivered)
    ];
}
