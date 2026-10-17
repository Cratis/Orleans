// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.StateMachines.for_StateMachine.when_transitioning_by_type;

public class and_both_deferred_targets_are_allowed : given.a_state_machine_with_deferred_transitions
{
    async Task Because()
    {
        await StateMachine.TransitionToStateOfType(typeof(StateThatSupportsTransitioningFrom)).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await StateMachine.TransitionToStateOfType(typeof(StateThatDoesNotSupportTransitioningFrom)).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _holding.Release.SetResult();
        await _transition.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_enter_both_targets_once_in_admission_order() => StateMachine.OnBeforeEnteringStates.Select(_ => _.GetType()).ShouldEqual([typeof(StateHoldingOnEnter), typeof(StateThatSupportsTransitioningFrom), typeof(StateThatDoesNotSupportTransitioningFrom)]);
    [Fact] async Task should_end_up_in_the_second_target() => (await StateMachine.GetCurrentState()).ShouldBeOfExactType<StateThatDoesNotSupportTransitioningFrom>();
}
