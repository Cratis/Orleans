// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines.Orders;

namespace Cratis.Orleans.Chronicle.StateMachines.for_StateMachineTransition.when_checking_whether_it_is_allowed;

public class and_it_is_not_guarded_and_the_model_has_no_instance : Specification
{
    StateMachineTransition<Order> _transition;
    bool _result;

    void Establish() => _transition = new(typeof(OrderEscalated), typeof(Escalated), []);

    void Because() => _result = _transition.IsAllowedFor(null);

    [Fact] void should_be_allowed() => _result.ShouldBeTrue();
}
