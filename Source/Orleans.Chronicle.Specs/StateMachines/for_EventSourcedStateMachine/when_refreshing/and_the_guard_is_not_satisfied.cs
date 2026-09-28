// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines.Orders;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachine.when_refreshing;

public class and_the_guard_is_not_satisfied : given.an_order_process
{
    async Task Establish()
    {
        _models.Enqueue(new("Jane", OrderStatus.Placed, []));
        _models.Enqueue(new("Jane", OrderStatus.Approved, []));
        await Activate();
    }

    Task Because() => Deliver(new OrderApproved());

    [Fact] void should_still_tell_about_the_model_change() => _stateMachine.ModelChanges.Count.ShouldEqual(1);
    [Fact] async Task should_stay_in_its_state() => (await CurrentState()).ShouldEqual(typeof(Placed));
}
