// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.StateMachines.for_StateMachine.given;

public abstract class a_state_machine_with_deferred_transitions : a_state_machine
{
    protected StateHoldingOnEnter _holding;
    protected Task _transition;

    protected override IEnumerable<IState<StateMachineStateForTesting>> CreateStates()
    {
        _holding = new();
        return [_holding, new StateThatDoesNotSupportTransitioningFrom(), new StateThatSupportsTransitioningFrom(), new StateWithAllowedTransitionState()];
    }

    async Task Establish()
    {
        _transition = StateMachine.TransitionTo<StateHoldingOnEnter>();
        await _holding.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    void Destroy() => _holding.Release.TrySetResult();
}
