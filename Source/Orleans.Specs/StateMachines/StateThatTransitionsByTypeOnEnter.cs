// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.StateMachines;

/// <summary>
/// A state that, while being entered, asks its state machine for a transition identified only by a <see cref="Type"/>.
/// </summary>
public class StateThatTransitionsByTypeOnEnter : BaseState
{
    public override Task<bool> CanTransitionTo<TState>(StateMachineStateForTesting state) => Task.FromResult(true);

    public override async Task<StateMachineStateForTesting> OnEnter(StateMachineStateForTesting state)
    {
        await ((StateMachineForTesting)StateMachine).TransitionToStateOfType(typeof(StateThatTransitionsOnLeave));
        return await base.OnEnter(state);
    }
}
