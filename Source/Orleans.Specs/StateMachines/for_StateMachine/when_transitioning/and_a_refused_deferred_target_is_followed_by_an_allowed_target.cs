// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.StateMachines.for_StateMachine.when_transitioning;

public class and_a_refused_deferred_target_is_followed_by_an_allowed_target : given.a_state_machine_with_deferred_transitions
{
    async Task Because()
    {
        await StateMachine.TransitionTo<StateWithAllowedTransitionState>().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await StateMachine.TransitionTo<StateThatDoesNotSupportTransitioningFrom>().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await StateMachine.TransitionTo<StateThatSupportsTransitioningFrom>().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _holding.Release.SetResult();
        await _transition.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_skip_the_refused_target_and_continue_in_order() => StateMachine.OnBeforeEnteringStates.Select(_ => _.GetType()).ShouldEqual([typeof(StateHoldingOnEnter), typeof(StateWithAllowedTransitionState), typeof(StateThatSupportsTransitioningFrom)]);
    [Fact] async Task should_enter_the_later_allowed_target() => (await StateMachine.GetCurrentState()).ShouldBeOfExactType<StateThatSupportsTransitioningFrom>();
}
