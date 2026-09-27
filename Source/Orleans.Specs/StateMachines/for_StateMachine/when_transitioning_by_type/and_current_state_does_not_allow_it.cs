// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Cratis.Orleans.StateMachines.for_StateMachine.when_transitioning_by_type;

public class and_current_state_does_not_allow_it : given.a_state_machine_with_well_known_states
{
    protected override Type InitialState => typeof(StateThatDoesNotSupportTransitioningFrom);

    void Establish()
    {
        _ = StateMachine;
        _silo.StorageStats<StateMachineForTesting, StateMachineStateForTesting>().ResetCounts();
        on_calls.Clear();
    }

    async Task Because() => await StateMachine.TransitionToStateOfType(typeof(StateThatSupportsTransitioningFrom));

    [Fact] async Task should_remain_in_the_current_state() => (await StateMachine.GetCurrentState()).ShouldBeOfExactType<StateThatDoesNotSupportTransitioningFrom>();
    [Fact] void should_not_leave_or_enter_any_state() => on_calls.ShouldBeEmpty();
    [Fact] void should_not_write_state() => _silo.StorageStats<StateMachineForTesting, StateMachineStateForTesting>().Writes.ShouldEqual(0);
}
