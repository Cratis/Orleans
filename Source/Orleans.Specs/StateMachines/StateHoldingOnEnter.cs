// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.StateMachines;

public class StateHoldingOnEnter : State<StateMachineStateForTesting>
{
    public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool AllowTransitions { get; set; } = true;
    public Func<Task> AfterRelease { get; set; } = () => Task.CompletedTask;

    public override Task<bool> CanTransitionTo<TTargetState>(StateMachineStateForTesting state) => Task.FromResult(AllowTransitions);

    public override async Task<StateMachineStateForTesting> OnEnter(StateMachineStateForTesting state)
    {
        Entered.SetResult();
        await Release.Task;
        await AfterRelease();
        return state;
    }
}
