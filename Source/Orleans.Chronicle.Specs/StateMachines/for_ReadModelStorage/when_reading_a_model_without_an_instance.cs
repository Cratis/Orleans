// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Cratis.Orleans.Chronicle.StateMachines.Orders;

namespace Cratis.Orleans.Chronicle.StateMachines.for_ReadModelStorage;

public class when_reading_a_model_without_an_instance : given.a_read_model_storage
{
    void Establish()
    {
        _eventStores.For("tenant-b").ReadModels.GetInstanceById<Order>(Arg.Any<ReadModelKey>()).Returns((Order)null!);
        _storage.Bind(_eventStores.Provider, _key);
    }

    Task Because() => _storage.ReadStateAsync();

    [Fact] void should_hold_no_model() => _storage.State.ShouldBeNull();
    [Fact] void should_say_the_record_does_not_exist() => _storage.RecordExists.ShouldBeFalse();
}
