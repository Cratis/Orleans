// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Orleans.Chronicle.StateMachines.Tickets;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachineRegistrar.when_registering;

public class a_state_machine_defining_its_projection : given.registrations
{
    Action<IProjectionBuilderFor<Ticket>> _registeredDefinition;
    IProjectionBuilderFor<Ticket> _projectionBuilder;

    void Establish()
    {
        _projectionBuilder = Substitute.For<IProjectionBuilderFor<Ticket>>();
        _registrations
            .When(_ => _.RegisterProjection(Arg.Any<Action<IProjectionBuilderFor<Ticket>>>()))
            .Do(call => _registeredDefinition = call.Arg<Action<IProjectionBuilderFor<Ticket>>>());
    }

    void Because()
    {
        Register(typeof(TicketLifecycle));
        _registeredDefinition(_projectionBuilder);
    }

    [Fact] void should_register_the_projection_once() => _registrations.Received(1).RegisterProjection(Arg.Any<Action<IProjectionBuilderFor<Ticket>>>());
    [Fact] void should_register_the_projection_the_state_machine_defines() => _projectionBuilder.Received(1).From<TicketOpened>(Arg.Any<Action<IFromBuilder<Ticket, TicketOpened>>?>());
    [Fact] void should_not_register_a_model_bound_read_model() => _registrations.DidNotReceive().RegisterReadModel<Ticket>();
    [Fact] void should_register_one_reactor_for_the_state_machine() => _reactors.Keys.ShouldContainOnly(new Cratis.Chronicle.Reactors.ReactorId(typeof(TicketLifecycle).FullName!));
}
