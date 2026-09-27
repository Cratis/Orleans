// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines.Orders;

namespace Cratis.Orleans.Chronicle.StateMachines.for_StateMachineTransition.when_checking_whether_it_is_allowed;

public class and_the_model_satisfies_every_guard : Specification
{
    StateMachineTransition<Order> _transition;
    bool _result;

    void Establish() => _transition = new(typeof(OrderApproved), typeof(Approved), [_ => _.Lines.Any(), _ => _.Customer.Length > 0]);

    void Because() => _result = _transition.IsAllowedFor(new("Jane", OrderStatus.Placed, [new("sku-1")]));

    [Fact] void should_be_allowed() => _result.ShouldBeTrue();
}
