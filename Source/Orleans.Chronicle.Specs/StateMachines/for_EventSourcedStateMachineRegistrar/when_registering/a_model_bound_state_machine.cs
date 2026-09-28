// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Orleans.Chronicle.StateMachines.Orders;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachineRegistrar.when_registering;

public class a_model_bound_state_machine : given.registrations
{
    void Because() => Register(typeof(OrderProcess));

    [Fact] void should_register_the_model_bound_read_model_once() => _registrations.Received(1).RegisterReadModel<Order>();
    [Fact] void should_not_register_a_projection_defined_in_code() => _registrations.DidNotReceive().RegisterProjection(Arg.Any<Action<IProjectionBuilderFor<Order>>>());
    [Fact] void should_register_one_reactor() => _reactors.Count.ShouldEqual(1);
}
