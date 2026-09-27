// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Orleans.Chronicle.StateMachines.Shipments;

public class ShipmentProjection : IProjectionFor<Shipment>
{
    public void Define(IProjectionBuilderFor<Shipment> builder) => builder
        .From<ShipmentDispatched>()
        .From<ShipmentDelivered>(_ => _.Set(shipment => shipment.Delivered).ToValue(true));
}
