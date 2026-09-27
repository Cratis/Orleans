// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Orleans.StateMachines;

namespace Cratis.Orleans.Chronicle.StateMachines.Orders;

public class Placed : State<Order>
{
    protected override IImmutableList<Type> AllowedTransitions => [typeof(Approved), typeof(Closed), typeof(Escalated)];
}
