// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines.Orders;

namespace Cratis.Orleans.Chronicle.StateMachines.for_ReadModelStorage.given;

public class a_read_model_storage : Specification
{
    protected EventStoresForTesting _eventStores;
    protected ReadModelStorage<Order> _storage;
    protected StateMachineKey _key;

    void Establish()
    {
        _eventStores = new();
        _key = new("some-store", "tenant-b", "order-1");
        _storage = new();
    }
}
