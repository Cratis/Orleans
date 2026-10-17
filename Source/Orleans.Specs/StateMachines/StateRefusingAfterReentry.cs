// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.StateMachines;

public class StateRefusingAfterReentry : State<StateMachineStateForTesting>
{
    public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int Entries { get; private set; }
    public int RefusedTargetGuardCalls { get; private set; }

    public override Task<bool> CanTransitionTo<TTargetState>(StateMachineStateForTesting state)
    {
        if (typeof(TTargetState) != typeof(StateThatDoesNotSupportTransitioningFrom))
        {
            return Task.FromResult(true);
        }

        RefusedTargetGuardCalls++;
        return Task.FromResult(state.Something != "refuse after reentry");
    }

    public override async Task<StateMachineStateForTesting> OnEnter(StateMachineStateForTesting state)
    {
        Entries++;
        if (Entries == 1)
        {
            Entered.SetResult();
            await Release.Task;
            return state;
        }

        return new StateMachineStateForTesting { Something = "refuse after reentry" };
    }
}
