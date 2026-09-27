// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;
using Cratis.Orleans.Chronicle.StateMachines.Orders;
using Cratis.Orleans.Chronicle.StateMachines.Tickets;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachineRegistrar.when_registering;

public class two_state_machines_over_different_models : given.registrations
{
    void Because() => Register(typeof(OrderProcess), typeof(TicketLifecycle));

    [Fact] void should_register_a_reactor_for_each() => _reactors.Keys.ShouldContainOnly(new ReactorId(typeof(OrderProcess).FullName!), new ReactorId(typeof(TicketLifecycle).FullName!));
    [Fact] void should_create_each_reactor_for_its_own_state_machine() => _reactorsCreatedFor.Select(_ => _.StateMachineType).ShouldContainOnly(typeof(OrderProcess), typeof(TicketLifecycle));
}
