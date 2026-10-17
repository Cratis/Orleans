// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.StateMachines.for_StateMachine.when_transitioning_by_type;

public class and_deferred_transitions_return_to_the_admitting_state : given.a_state_machine_with_reentry
{
    async Task Because()
    {
        await StateMachine.TransitionToStateOfType(typeof(StateThatSupportsTransitioningFrom)).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await StateMachine.TransitionToStateOfType(typeof(StateRefusingAfterReentry)).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await StateMachine.TransitionToStateOfType(typeof(StateThatDoesNotSupportTransitioningFrom)).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _holding.Release.SetResult();
        await _transition.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_reenter_the_original_state() => _holding.Entries.ShouldEqual(2);
    [Fact] void should_check_the_later_target_again_after_returning() => _holding.RefusedTargetGuardCalls.ShouldEqual(2);
    [Fact] void should_skip_the_target_disallowed_after_returning() => StateMachine.OnBeforeEnteringStates.Select(_ => _.GetType()).ShouldEqual([typeof(StateRefusingAfterReentry), typeof(StateThatSupportsTransitioningFrom), typeof(StateRefusingAfterReentry)]);
    [Fact] async Task should_remain_in_the_reentered_state() => (await StateMachine.GetCurrentState()).ShouldBeOfExactType<StateRefusingAfterReentry>();
}
