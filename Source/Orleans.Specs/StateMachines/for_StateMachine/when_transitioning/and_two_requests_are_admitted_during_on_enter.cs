// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.StateMachines.for_StateMachine.when_transitioning;

public class and_two_requests_are_admitted_during_on_enter : given.a_state_machine_with_deferred_transitions
{
    bool _ownerWasStillEntering;

    async Task Because()
    {
        await StateMachine.TransitionTo<StateThatDoesNotSupportTransitioningFrom>().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await StateMachine.TransitionTo<StateThatSupportsTransitioningFrom>().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _ownerWasStillEntering = !_transition.IsCompleted;
        _holding.Release.SetResult();
        await _transition.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_acknowledge_admission_without_waiting_for_the_active_enter() => _ownerWasStillEntering.ShouldBeTrue();
    [Fact] void should_skip_the_second_target_that_the_first_target_disallows() => StateMachine.OnBeforeEnteringStates.Select(_ => _.GetType()).ShouldEqual([typeof(StateHoldingOnEnter), typeof(StateThatDoesNotSupportTransitioningFrom)]);
    [Fact] async Task should_remain_in_the_first_target() => (await StateMachine.GetCurrentState()).ShouldBeOfExactType<StateThatDoesNotSupportTransitioningFrom>();
}
