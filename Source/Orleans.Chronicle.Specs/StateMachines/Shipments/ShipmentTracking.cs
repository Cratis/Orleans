// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Orleans.StateMachines;

namespace Cratis.Orleans.Chronicle.StateMachines.Shipments;

public class ShipmentTracking : EventSourcedStateMachine<Shipment>
{
    public override IImmutableList<IState<Shipment>> CreateStates() => [];
}
