// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.StateMachines.for_StateMachine.when_transitioning_by_type;

public class and_it_is_requested_while_entering_a_state : given.a_state_machine_with_well_known_states
{
    protected override Type InitialState => typeof(StateThatSupportsTransitioningFrom);

    void Establish()
    {
        _ = StateMachine;
        on_calls.Clear();
    }

    async Task Because() => await StateMachine.TransitionToStateOfType(typeof(StateThatTransitionsByTypeOnEnter));

    [Fact] void should_finish_entering_the_state_before_transitioning() => (on_calls[1].Type == typeof(StateThatTransitionsByTypeOnEnter) && on_calls[1].IsEnter).ShouldBeTrue();
    [Fact] void should_leave_the_state_before_transitioning() => (on_calls[2].Type == typeof(StateThatTransitionsByTypeOnEnter) && !on_calls[2].IsEnter).ShouldBeTrue();
    [Fact] void should_transition_to_the_requested_state_after_the_first_transition_is_done() => (on_calls[3].Type == typeof(StateThatTransitionsOnLeave) && on_calls[3].IsEnter).ShouldBeTrue();
    [Fact] async Task should_end_up_in_the_requested_state() => (await StateMachine.GetCurrentState()).ShouldBeOfExactType<StateThatTransitionsOnLeave>();
    [Fact] void should_write_current_state() => _stateStorage.State.CurrentState.ShouldEqual(typeof(StateThatTransitionsOnLeave).FullName);
}
