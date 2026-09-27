// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines.Orders;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachine.when_activating;

public class without_a_model_instance : given.an_order_process
{
    void Establish() => _models.Enqueue(null);

    Task Because() => Activate();

    [Fact] async Task should_be_in_the_initial_state() => (await CurrentState()).ShouldEqual(typeof(Placed));
}
