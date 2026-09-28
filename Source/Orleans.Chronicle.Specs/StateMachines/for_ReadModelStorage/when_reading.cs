// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Cratis.Orleans.Chronicle.StateMachines.Orders;

namespace Cratis.Orleans.Chronicle.StateMachines.for_ReadModelStorage;

public class when_reading : given.a_read_model_storage
{
    Order _model;

    void Establish()
    {
        _model = new("Jane", OrderStatus.Approved, []);
        _eventStores.For("tenant-b").ReadModels.GetInstanceById<Order>(Arg.Any<ReadModelKey>()).Returns(_model);
        _storage.Bind(_eventStores.Provider, _key);
    }

    Task Because() => _storage.ReadStateAsync();

    [Fact] void should_read_from_the_event_store_and_namespace_of_the_key() => _eventStores.Provider.Received(1).Get(_key.EventStore, _key.Namespace);
    [Fact] void should_read_the_model_for_the_event_source_of_the_key() => _eventStores.For("tenant-b").ReadModels.Received(1).GetInstanceById<Order>(new ReadModelKey("order-1"));
    [Fact] void should_hold_the_model_read() => _storage.State.ShouldEqual(_model);
    [Fact] void should_say_the_record_exists() => _storage.RecordExists.ShouldBeTrue();
    [Fact] void should_not_read_from_any_other_namespace() => _eventStores.Provider.DidNotReceive().Get(Arg.Any<Cratis.Chronicle.EventStoreName>(), Arg.Is<Cratis.Chronicle.EventStoreNamespaceName>(_ => _.Value != "tenant-b"));
}
