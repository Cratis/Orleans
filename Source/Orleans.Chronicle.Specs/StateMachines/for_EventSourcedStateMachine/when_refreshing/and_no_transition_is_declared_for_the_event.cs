// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines.Orders;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachine.when_refreshing;

public class and_no_transition_is_declared_for_the_event : given.an_order_process
{
    Order _after;

    async Task Establish()
    {
        _after = new("Jane", OrderStatus.Placed, [new("sku-1")]);
        _models.Enqueue(new("Jane", OrderStatus.Placed, []));
        _models.Enqueue(_after);
        await Activate();
    }

    Task Because() => Deliver(new LineAdded("sku-1"));

    [Fact] void should_tell_about_the_refreshed_model() => OnlyModelChange.Current.ShouldEqual(_after);
    [Fact] async Task should_stay_in_its_state() => (await CurrentState()).ShouldEqual(typeof(Placed));
}
