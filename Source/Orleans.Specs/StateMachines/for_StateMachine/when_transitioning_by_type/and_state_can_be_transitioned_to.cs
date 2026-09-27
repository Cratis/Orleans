// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Cratis.Orleans.StateMachines.for_StateMachine.when_transitioning_by_type;

public class and_state_can_be_transitioned_to : given.a_state_machine_with_well_known_states
{
    protected override Type InitialState => typeof(StateThatSupportsTransitioningFrom);

    void Establish()
    {
        _ = StateMachine;
        _silo.StorageStats<StateMachineForTesting, StateMachineStateForTesting>().ResetCounts();
        on_calls.Clear();
    }

    async Task Because() => await StateMachine.TransitionToStateOfType(typeof(StateThatDoesNotSupportTransitioningFrom));

    [Fact] async Task should_transition_to_the_state() => (await StateMachine.GetCurrentState()).ShouldBeOfExactType<StateThatDoesNotSupportTransitioningFrom>();
    [Fact] void should_only_have_two_on_calls() => on_calls.Count.ShouldEqual(2);
    [Fact] void should_leave_the_state_transitioned_from() => (on_calls[0].Type == typeof(StateThatSupportsTransitioningFrom) && !on_calls[0].IsEnter).ShouldBeTrue();
    [Fact] void should_enter_the_state_transitioned_to() => (on_calls[1].Type == typeof(StateThatDoesNotSupportTransitioningFrom) && on_calls[1].IsEnter).ShouldBeTrue();
    [Fact] void should_write_state_once() => _silo.StorageStats<StateMachineForTesting, StateMachineStateForTesting>().Writes.ShouldEqual(1);
    [Fact] void should_write_current_state() => _stateStorage.State.CurrentState.ShouldEqual(typeof(StateThatDoesNotSupportTransitioningFrom).FullName);
}
