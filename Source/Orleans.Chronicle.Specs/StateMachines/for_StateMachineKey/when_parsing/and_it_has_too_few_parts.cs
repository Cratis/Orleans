// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Chronicle.StateMachines.for_StateMachineKey.when_parsing;

public class and_it_has_too_few_parts : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => StateMachineKey.Parse("tenant-a/order-1"));

    [Fact] void should_throw_invalid_state_machine_key() => _error.ShouldBeOfExactType<InvalidStateMachineKey>();
}
