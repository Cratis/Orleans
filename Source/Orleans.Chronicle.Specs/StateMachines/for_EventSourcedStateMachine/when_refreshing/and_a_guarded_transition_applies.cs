// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines.Orders;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachine.when_refreshing;

public class and_a_guarded_transition_applies : given.an_order_process
{
    Order _before;
    Order _after;

    async Task Establish()
    {
        _before = new("Jane", OrderStatus.Placed, [new("sku-1")]);
        _after = _before with { Status = OrderStatus.Approved };
        _models.Enqueue(_before);
        _models.Enqueue(_after);
        await Activate();
    }

    Task Because() => Deliver(new OrderApproved(), sequenceNumber: 7);

    [Fact] void should_give_the_model_before_the_event_as_previous() => OnlyModelChange.Previous.ShouldEqual(_before);
    [Fact] void should_give_the_refreshed_model_as_current() => OnlyModelChange.Current.ShouldEqual(_after);
    [Fact] void should_give_the_context_of_the_event() => OnlyModelChange.Context.SequenceNumber.Value.ShouldEqual(7UL);
    [Fact] void should_tell_about_the_model_change_before_transitioning() => OnlyModelChange.StateWhenCalled.ShouldEqual(typeof(Placed));
    [Fact] async Task should_transition_to_the_state_of_the_transition() => (await CurrentState()).ShouldEqual(typeof(Approved));
}
