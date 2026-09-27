// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Chronicle.StateMachines.for_StateMachineKey.when_parsing;

public class a_formatted_key : Specification
{
    StateMachineKey _key;
    StateMachineKey _result;

    void Establish() => _key = new("some-store", "tenant-a", "order-1");

    void Because() => _result = StateMachineKey.Parse(_key.ToString());

    [Fact] void should_give_the_same_key() => _result.ShouldEqual(_key);
}
