// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Chronicle.StateMachines.for_StateMachineKey.when_formatting;

public class and_every_part_is_valid : Specification
{
    string _result;

    void Because() => _result = new StateMachineKey("some-store", "tenant-a", "order-1").ToString();

    [Fact] void should_join_event_store_namespace_and_event_source_id() => _result.ShouldEqual("some-store/tenant-a/order-1");
}
