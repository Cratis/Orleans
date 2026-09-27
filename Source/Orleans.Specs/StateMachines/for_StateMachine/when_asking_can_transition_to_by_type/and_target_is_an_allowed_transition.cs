// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.StateMachines.for_StateMachine.when_asking_can_transition_to_by_type;

public class and_target_is_an_allowed_transition : given.a_state_machine
{
    bool _result;

    protected override Type InitialState => typeof(StateWithAllowedTransitionState);

    protected override IEnumerable<IState<StateMachineStateForTesting>> CreateStates() =>
    [
        new StateWithAllowedTransitionState(),
        new StateThatSupportsTransitioningFrom(),
        new StateThatDoesNotSupportTransitioningFrom()
    ];

    async Task Because() => _result = await StateMachine.CanTransitionToStateOfType(typeof(StateThatSupportsTransitioningFrom));

    [Fact] void should_be_able_to_transition() => _result.ShouldBeTrue();
}
