// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines.Orders;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachine.when_refreshing;

public class and_the_model_has_its_first_instance : given.an_order_process
{
    Order _after;

    async Task Establish()
    {
        _after = new("Jane", OrderStatus.Placed, []);
        _models.Enqueue(null);
        _models.Enqueue(_after);
        await Activate();
    }

    Task Because() => Deliver(new OrderPlaced("Jane"));

    [Fact] void should_have_had_no_previous_model() => OnlyModelChange.Previous.ShouldBeNull();
    [Fact] void should_give_the_new_model_as_current() => OnlyModelChange.Current.ShouldEqual(_after);
}
