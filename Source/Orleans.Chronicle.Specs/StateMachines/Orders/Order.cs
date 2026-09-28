// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Orleans.Chronicle.StateMachines.Orders;

[Passive]
[FromEvent<OrderPlaced>]
public record Order(
    string Customer,
    [SetValue<OrderApproved>(OrderStatus.Approved)]
    [SetValue<OrderShipped>(OrderStatus.Closed)]
    [SetValue<OrderCancelled>(OrderStatus.Closed)]
    OrderStatus Status,
    [ChildrenFrom<LineAdded>(identifiedBy: nameof(OrderLine.Sku))]
    IEnumerable<OrderLine> Lines);
