// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines.Misfits;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachineInspector.when_inspecting;

public class a_state_machine_interested_in_no_events : given.client_artifacts
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => EventSourcedStateMachineInspector.Inspect(typeof(StateMachineWithoutEventInterest), _artifacts));

    [Fact] void should_throw_state_machine_has_no_event_interest() => _error.ShouldBeOfExactType<StateMachineHasNoEventInterest>();
}
