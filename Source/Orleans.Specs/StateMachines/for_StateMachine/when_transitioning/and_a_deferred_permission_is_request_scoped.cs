// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.StateMachines.for_StateMachine.when_transitioning;

public class and_a_deferred_permission_is_request_scoped : given.a_state_machine_with_deferred_transitions
{
    async Task Because()
    {
        _holding.AllowTransitions = false;
        _holding.AfterRelease = async () =>
        {
            _holding.AllowTransitions = true;
            try
            {
                await StateMachine.TransitionTo<StateThatSupportsTransitioningFrom>().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            }
            finally
            {
                _holding.AllowTransitions = false;
            }
        };
        _holding.Release.SetResult();
        await _transition.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_reset_the_permission_before_the_drain() => _holding.AllowTransitions.ShouldBeFalse();
    [Fact] void should_honor_the_admission_from_the_same_state() => StateMachine.OnBeforeEnteringStates.Select(_ => _.GetType()).ShouldEqual([typeof(StateHoldingOnEnter), typeof(StateThatSupportsTransitioningFrom)]);
    [Fact] async Task should_enter_the_admitted_target() => (await StateMachine.GetCurrentState()).ShouldBeOfExactType<StateThatSupportsTransitioningFrom>();
}
