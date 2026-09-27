// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines.Orders;

namespace Cratis.Orleans.Chronicle.StateMachines.for_ReadModelStorage;

public class when_writing_and_clearing : given.a_read_model_storage
{
    Order _model;

    void Establish()
    {
        _model = new("Jane", OrderStatus.Placed, []);
        _storage.Bind(_eventStores.Provider, _key);
        _storage.State = _model;
    }

    async Task Because()
    {
        await _storage.WriteStateAsync();
        await _storage.ClearStateAsync();
    }

    [Fact] void should_not_touch_any_event_store() => _eventStores.Provider.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_keep_the_model() => _storage.State.ShouldEqual(_model);
    [Fact] void should_have_no_etag() => _storage.Etag.ShouldBeNull();
}
