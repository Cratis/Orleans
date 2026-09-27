// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Orleans.Chronicle.StateMachines.Orders;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachineRegistrar.when_registering;

public class a_model_bound_state_machine_whose_model_chronicle_discovered : given.registrations
{
    void Establish() => _discoveredModelBoundProjections.Add(typeof(Order));

    void Because() => Register(typeof(OrderProcess));

    [Fact] void should_not_register_the_read_model_again() => _registrations.DidNotReceive().RegisterReadModel<Order>();
    [Fact] void should_not_register_a_projection() => _registrations.DidNotReceive().RegisterProjection(Arg.Any<Action<IProjectionBuilderFor<Order>>>());
    [Fact] void should_still_register_the_reactor() => _reactors.Count.ShouldEqual(1);
}
