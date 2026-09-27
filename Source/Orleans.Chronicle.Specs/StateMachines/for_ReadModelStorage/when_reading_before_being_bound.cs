// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Chronicle.StateMachines.for_ReadModelStorage;

public class when_reading_before_being_bound : given.a_read_model_storage
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(_storage.ReadStateAsync);

    [Fact] void should_throw_read_model_storage_is_not_bound() => _error.ShouldBeOfExactType<ReadModelStorageIsNotBound>();
}
