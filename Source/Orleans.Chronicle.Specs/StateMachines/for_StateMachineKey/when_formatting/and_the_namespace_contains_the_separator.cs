// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Chronicle.StateMachines.for_StateMachineKey.when_formatting;

public class and_the_namespace_contains_the_separator : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => new StateMachineKey("some-store", "tenant/a", "order-1").ToString());

    [Fact] void should_throw_invalid_state_machine_key() => _error.ShouldBeOfExactType<InvalidStateMachineKey>();
}
