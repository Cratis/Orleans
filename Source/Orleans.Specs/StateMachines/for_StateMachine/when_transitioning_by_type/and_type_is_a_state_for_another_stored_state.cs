// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.StateMachines.for_StateMachine.when_transitioning_by_type;

public class and_type_is_a_state_for_another_stored_state : given.a_state_machine_with_well_known_states
{
    protected override Type InitialState => typeof(StateThatSupportsTransitioningFrom);
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(() => StateMachine.TransitionToStateOfType(typeof(NoOpState<string>)));

    [Fact] void should_throw_invalid_type_for_state() => _exception.ShouldBeOfExactType<InvalidTypeForState>();
}
