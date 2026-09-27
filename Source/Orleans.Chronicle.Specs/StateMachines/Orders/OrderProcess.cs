// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Orleans.StateMachines;

namespace Cratis.Orleans.Chronicle.StateMachines.Orders;

public class OrderProcess : EventSourcedStateMachine<Order>
{
    public List<(Order? Previous, Order? Current, EventContext Context, Type StateWhenCalled)> ModelChanges { get; } = [];

    protected override Type InitialState => typeof(Placed);

    public override IImmutableList<IState<Order>> CreateStates() => [new Placed(), new Approved(), new Escalated(), new Closed()];

    protected override void DefineTransitions(IStateMachineTransitions<Order> transitions) => transitions
        .On<OrderApproved>().When(order => order.Lines.Any()).TransitionTo<Approved>()
        .On<IOrderClosed>().TransitionTo<Closed>()
        .On<OrderEscalated>().TransitionTo<Escalated>();

    protected override Type ResolveState(Order? model) => model?.Status switch
    {
        OrderStatus.Approved => typeof(Approved),
        OrderStatus.Closed => typeof(Closed),
        _ => InitialState
    };

    protected override async Task OnModelChanged(Order? previous, Order? current, EventContext context) =>
        ModelChanges.Add((previous, current, context, (await GetCurrentState()).GetType()));
}
