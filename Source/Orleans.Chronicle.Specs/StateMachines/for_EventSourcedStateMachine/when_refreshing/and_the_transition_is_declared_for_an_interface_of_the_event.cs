// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines.Orders;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachine.when_refreshing;

public class and_the_transition_is_declared_for_an_interface_of_the_event : given.an_order_process
{
    async Task Establish()
    {
        _models.Enqueue(new("Jane", OrderStatus.Approved, [new("sku-1")]));
        _models.Enqueue(new("Jane", OrderStatus.Closed, [new("sku-1")]));
        await Activate();
    }

    Task Because() => Deliver(new OrderCancelled());

    [Fact] async Task should_transition_to_the_state_of_the_transition() => (await CurrentState()).ShouldEqual(typeof(Closed));
}
