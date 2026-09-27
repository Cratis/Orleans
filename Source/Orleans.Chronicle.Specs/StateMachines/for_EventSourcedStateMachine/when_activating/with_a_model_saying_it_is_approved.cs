// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Cratis.Orleans.Chronicle.StateMachines.Orders;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachine.when_activating;

public class with_a_model_saying_it_is_approved : given.an_order_process
{
    Order _model;

    void Establish()
    {
        _model = new("Jane", OrderStatus.Approved, [new("sku-1")]);
        _models.Enqueue(_model);
    }

    Task Because() => Activate();

    [Fact] async Task should_be_in_the_state_the_model_says() => (await CurrentState()).ShouldEqual(typeof(Approved));
    [Fact] void should_read_the_model_for_its_event_source() => _eventStores.For("tenant-a").ReadModels.Received(1).GetInstanceById<Order>(new ReadModelKey("order-1"));
    [Fact] void should_read_from_the_namespace_in_its_key() => _eventStores.Provider.DidNotReceive().Get(Arg.Any<Cratis.Chronicle.EventStoreName>(), Arg.Is<Cratis.Chronicle.EventStoreNamespaceName>(_ => _.Value != "tenant-a"));
    [Fact] void should_not_tell_about_a_model_change_on_activation() => _stateMachine.ModelChanges.ShouldBeEmpty();
}
