// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.StateMachines.Misfits;

namespace Cratis.Orleans.Chronicle.StateMachines.for_EventSourcedStateMachineInspector.when_inspecting;

public class a_state_machine_nothing_defines_a_projection_for : given.client_artifacts
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => EventSourcedStateMachineInspector.Inspect(typeof(StateMachineWithoutProjection), _artifacts));

    [Fact] void should_throw_missing_projection_for_state_machine_model() => _error.ShouldBeOfExactType<MissingProjectionForStateMachineModel>();
}
